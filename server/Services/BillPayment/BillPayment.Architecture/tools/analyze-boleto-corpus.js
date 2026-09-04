#!/usr/bin/env node
/**
 * Mede um corpus de boletos em PDF: taxa de extração da linha digitável,
 * PDFs sem camada de texto (precisam de OCR) e presença do CNPJ/CPF do pagador.
 *
 * Gera os números de ../08-boleto-corpus-findings.md. Reexecute quando o corpus
 * mudar ou quando o parser real existir, e atualize aquele documento.
 *
 * Uso:
 *   pdftotext -layout -enc UTF-8 <cada>.pdf <saida>.txt      # requer poppler
 *   node analyze-boleto-corpus.js <pasta-com-os-txt> [cnpj-do-pagador ...]
 *
 * No Windows, o pdftotext.exe já vem com o Git em Program Files/Git/mingw64/bin.
 */
const fs = require('fs'), path = require('path');

// ---------- dígitos verificadores ----------

const mod10 = s => {
  let t = 0, m = 2;
  for (let i = s.length - 1; i >= 0; i--) {
    let p = +s[i] * m; if (p > 9) p = Math.floor(p / 10) + (p % 10);
    t += p; m = m === 2 ? 1 : 2;
  }
  return (10 - (t % 10)) % 10;
};

const sumWeighted = s => { // pesos 2..9 da direita para a esquerda
  let t = 0, w = 2;
  for (let i = s.length - 1; i >= 0; i--) { t += +s[i] * w; w = w === 9 ? 2 : w + 1; }
  return t;
};

const mod11Barcode = s => { // DV geral do código de barras de cobrança
  const r = 11 - (sumWeighted(s) % 11);
  return (r === 0 || r === 10 || r === 11) ? 1 : r;
};

// A especificação FEBRABAN de arrecadação tem implementações divergentes em
// campo; aceitamos as variantes conhecidas em vez de rejeitar boleto válido.
const mod11ArrCandidates = s => {
  const r = sumWeighted(s) % 11, d = 11 - r;
  return new Set([d > 9 ? 0 : d, d > 9 ? 1 : d, r <= 1 ? 0 : d]);
};

// ---------- fator de vencimento ----------

const EPOCH_1997 = Date.UTC(1997, 9, 7);   // fator 1 = 08/10/1997
const EPOCH_2025 = Date.UTC(2025, 1, 22);  // fator 1000 = 22/02/2025 (rollover)
const DAY = 864e5;

/**
 * O fator tem 4 dígitos e já deu a volta uma vez, então 1000..9999 é ambíguo:
 * 1493 pode ser 2001-11-08 (base 1997) ou 2026-06-30 (base 2025).
 *
 * Em vez de fixar uma base — que quebra no próximo rollover — geramos os
 * candidatos e escolhemos o mais próximo de hoje. Um vencimento de boleto está
 * sempre a poucos anos do presente, então o critério é estável e se corrige
 * sozinho nos ciclos futuros.
 */
function dueDateFromFactor(fator, today = new Date()) {
  if (fator === 0) return null; // 0000 = sem vencimento; não vira data
  const cands = [EPOCH_1997 + fator * DAY];
  if (fator >= 1000) cands.push(EPOCH_2025 + (fator - 1000) * DAY);
  const now = today.getTime();
  const best = cands.reduce((a, b) => Math.abs(b - now) < Math.abs(a - now) ? b : a);
  return new Date(best).toISOString().slice(0, 10);
}

// ---------- linha digitável ----------

function parseCobranca(l) { // 47 dígitos
  if (!/^\d{47}$/.test(l)) return null;
  const c1 = l.slice(0, 9), c2 = l.slice(10, 20), c3 = l.slice(21, 31);
  if (mod10(c1) !== +l[9] || mod10(c2) !== +l[20] || mod10(c3) !== +l[31]) return null;
  const dv = +l[32];
  const bc = l.slice(0, 4) + dv + l.slice(33) + l.slice(4, 9) + c2 + c3;
  if (bc.length !== 44) return null;
  if (mod11Barcode(bc.slice(0, 4) + bc.slice(5)) !== dv) return null;
  return {
    kind: 'cobranca', bank: bc.slice(0, 3),
    amount: +bc.slice(9, 19) / 100,
    dueDate: dueDateFromFactor(+bc.slice(5, 9)),
    barcode: bc,
  };
}

function parseArrecadacao(l) { // 48 dígitos
  if (!/^8\d{47}$/.test(l)) return null;
  const bc = [0, 1, 2, 3].map(i => l.slice(i * 12, i * 12 + 11)).join('');
  const id = bc[2], useMod10 = id === '6' || id === '7';
  for (let i = 0; i < 4; i++) {
    const blk = l.slice(i * 12, i * 12 + 11), dv = +l[i * 12 + 11];
    if (useMod10 ? mod10(blk) !== dv : !mod11ArrCandidates(blk).has(dv)) return null;
  }
  return { kind: 'arrecadacao', segment: bc[1], valueId: id, amount: +bc.slice(4, 15) / 100, barcode: bc };
}

/** Todo candidato é hipótese até o DV fechar — nunca "pegue o primeiro match". */
function extractDigitableLine(text) {
  const flat = text.replace(/[.\-\s/]/g, '');
  for (const m of flat.matchAll(/\d{47,}/g)) {
    const s = m[0];
    for (let i = 0; i + 47 <= s.length; i++) {
      const cobranca = s.substr(i, 47);
      const arrecadacao = s.substr(i, 48);
      const hit = parseCobranca(cobranca);
      if (hit) return { ...hit, digitableLine: cobranca };
      const util = parseArrecadacao(arrecadacao);
      if (util) return { ...util, digitableLine: arrecadacao };
    }
  }
  return null;
}

// ---------- CPF / CNPJ ----------

const validCnpj = d => {
  if (!/^\d{14}$/.test(d) || /^(\d)\1{13}$/.test(d)) return false;
  const calc = len => {
    let w = len - 7, t = 0;
    for (let i = 0; i < len; i++) { t += +d[i] * w--; if (w < 2) w = 9; }
    const r = t % 11; return r < 2 ? 0 : 11 - r;
  };
  return calc(12) === +d[12] && calc(13) === +d[13];
};

const validCpf = d => {
  if (!/^\d{11}$/.test(d) || /^(\d)\1{10}$/.test(d)) return false;
  const calc = len => {
    let t = 0;
    for (let i = 0; i < len; i++) t += +d[i] * (len + 1 - i);
    const r = (t * 10) % 11; return r === 10 ? 0 : r;
  };
  return calc(9) === +d[9] && calc(10) === +d[10];
};

/** Sem validar o DV, fonte de código de barras renderizada como texto vira
 *  centenas de falsos CNPJs — aconteceu no corpus real. */
function extractTaxIds(text) {
  const out = new Set();
  for (const m of text.matchAll(/\d{2}[.\s]?\d{3}[.\s]?\d{3}[/\s]?\d{4}[-\s]?\d{2}/g)) {
    const d = m[0].replace(/\D/g, ''); if (validCnpj(d)) out.add(d);
  }
  for (const m of text.matchAll(/\d{3}[.\s]?\d{3}[.\s]?\d{3}[-\s]?\d{2}/g)) {
    const d = m[0].replace(/\D/g, ''); if (validCpf(d)) out.add(d);
  }
  return out;
}

// ---------- relatório ----------

const argv = process.argv.slice(2);
const asJson = argv.includes('--json');           // emite as linhas extraídas para outra ferramenta consumir
const positional = argv.filter(a => !a.startsWith('--'));

const dir = positional[0];
if (!dir) { console.error('uso: node analyze-boleto-corpus.js <pasta-txt> [--json] [taxId-do-pagador ...]'); process.exit(1); }
const payers = positional.slice(1).map(s => s.replace(/\D/g, ''));

if (asJson) {
  const out = [];
  for (const f of fs.readdirSync(dir).filter(f => f.endsWith('.txt')).sort()) {
    const text = fs.readFileSync(path.join(dir, f), 'utf8');
    if (!text.trim()) continue;
    const line = extractDigitableLine(text);
    if (line) out.push({ file: f.replace(/\.txt$/, ''), ...line });
  }
  console.log(JSON.stringify(out, null, 2));
  process.exit(0);
}
const mask = s => s.slice(0, 5) + '***' + s.slice(-4);

const stats = { total: 0, parsed: 0, imageOnly: 0, noLine: 0, withPayer: 0, cobranca: 0, arrecadacao: 0 };

for (const f of fs.readdirSync(dir).filter(f => f.endsWith('.txt')).sort()) {
  const text = fs.readFileSync(path.join(dir, f), 'utf8');
  const name = f.replace(/\.txt$/, '');
  stats.total++;

  if (!text.trim()) { stats.imageOnly++; console.log(`${name.padEnd(44)} PDF-IMAGEM (precisa OCR)`); continue; }

  const line = extractDigitableLine(text);
  const ids = extractTaxIds(text);
  const payerHit = payers.filter(p => ids.has(p) || (p.length === 14 && [...ids].some(i => i.startsWith(p.slice(0, 8)))));
  if (payerHit.length) stats.withPayer++;

  if (!line) { stats.noLine++; console.log(`${name.padEnd(44)} SEM LINHA VALIDA`.padEnd(70) + (payerHit.length ? `pagador=${mask(payerHit[0])}` : '')); continue; }

  stats.parsed++;
  stats[line.kind]++;
  const detail = line.kind === 'cobranca'
    ? `banco=${line.bank} valor=${line.amount.toFixed(2)} venc=${line.dueDate}`
    : `seg=${line.segment} id=${line.valueId} valor=${line.amount.toFixed(2)}`;
  console.log(`${name.padEnd(44)} ${line.kind.toUpperCase().padEnd(12)} ${detail.padEnd(45)}` + (payerHit.length ? `pagador=${mask(payerHit[0])}` : ''));
}

const pct = n => `${((n / stats.total) * 100).toFixed(0)}%`;
console.log(`
=== RESUMO ===
total .................. ${stats.total}
linha valida ........... ${stats.parsed} (${pct(stats.parsed)})   cobranca=${stats.cobranca} arrecadacao=${stats.arrecadacao}
PDF sem texto (OCR) .... ${stats.imageOnly} (${pct(stats.imageOnly)})
texto sem linha ........ ${stats.noLine} (${pct(stats.noLine)})
pagador identificado ... ${stats.withPayer} (${pct(stats.withPayer)})`);
