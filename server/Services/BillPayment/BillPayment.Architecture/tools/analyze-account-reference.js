#!/usr/bin/env node
/**
 * Mede se existe REFERÊNCIA DE CONTA estável num boleto — a chave de que o degrau 2
 * da escada de roteamento depende (`RoutingRule` por (PayeeTaxId, AccountReference),
 * doc 07). Se nada no documento identificar a conta de forma estável entre meses, a
 * regra aprendida nunca casa e o degrau 2 não existe na prática.
 *
 * O que ele faz: agrupa boletos da MESMA conta em meses diferentes (o corpus nomeia
 * os arquivos por fornecedor) e compara, posição a posição, o campo livre do código
 * de barras. O que sobra igual em todos os meses é candidato a referência de conta.
 *
 * Uso:
 *   pdftotext -layout -enc UTF-8 <cada>.pdf <saida>.txt     # requer poppler
 *   node analyze-account-reference.js <pasta-com-os-txt>
 *
 * Convenção do nome do .txt: <mes>__<subpastas>__<arquivo>.txt (o mês vira o eixo
 * temporal do agrupamento).
 */
const fs = require('fs'), path = require('path');

// ---------- dígitos verificadores (mesma aritmética do analyze-boleto-corpus.js) ----------

const mod10 = s => {
  let t = 0, m = 2;
  for (let i = s.length - 1; i >= 0; i--) {
    let p = +s[i] * m; if (p > 9) p = Math.floor(p / 10) + (p % 10);
    t += p; m = m === 2 ? 1 : 2;
  }
  return (10 - (t % 10)) % 10;
};

const sumWeighted = s => {
  let t = 0, w = 2;
  for (let i = s.length - 1; i >= 0; i--) { t += +s[i] * w; w = w === 9 ? 2 : w + 1; }
  return t;
};

const mod11Barcode = s => {
  const r = 11 - (sumWeighted(s) % 11);
  return (r === 0 || r === 10 || r === 11) ? 1 : r;
};

const mod11ArrCandidates = s => {
  const r = sumWeighted(s) % 11, d = 11 - r;
  return new Set([d > 9 ? 0 : d, d > 9 ? 1 : d, r <= 1 ? 0 : d]);
};

// ---------- linha digitável -> código de barras ----------

// Cobrança: 47 dígitos em 5 campos, cada um com DV mod10; o DV geral fica na pos 5.
function bankSlipBarcode(l) {
  if (l.length !== 47) return null;
  const f1 = l.slice(0, 9), d1 = l[9];
  const f2 = l.slice(10, 20), d2 = l[20];
  const f3 = l.slice(21, 31), d3 = l[31];
  const dv = l[32], f5 = l.slice(33);

  if (mod10(f1) !== +d1 || mod10(f2) !== +d2 || mod10(f3) !== +d3) return null;

  const barcode = f1.slice(0, 4) + dv + f5 + f1.slice(4) + f2 + f3;
  if (barcode.length !== 44) return null;
  if (mod11Barcode(barcode.slice(0, 4) + barcode.slice(5)) !== +dv) return null;

  return barcode;
}

// Arrecadação: 48 dígitos em 4 blocos de 11 + DV de bloco; o DV geral fica na pos 4.
function utilityBarcode(l) {
  if (l.length !== 48) return null;

  const blocks = [l.slice(0, 12), l.slice(12, 24), l.slice(24, 36), l.slice(36, 48)];
  const mode = l[2]; // id do valor: 6/7 = mod10, 8/9 = mod11
  const barcode = blocks.map(b => b.slice(0, 11)).join('');
  if (barcode.length !== 44) return null;

  for (const b of blocks) {
    const body = b.slice(0, 11), d = +b[11];
    const ok = (mode === '6' || mode === '7') ? mod10(body) === d : mod11ArrCandidates(body).has(d);
    if (!ok) return null;
  }

  const dv = +barcode[3];
  if (!mod11ArrCandidates(barcode.slice(0, 3) + barcode.slice(4)).has(dv)) return null;

  return barcode;
}

// ---------- varredura ----------

// Não existe regex confiável para linha digitável: ela vem com pontos, espaços,
// quebrada ou colada em outro número. Geramos todas as janelas e deixamos o DV
// reprovar — mesma doutrina do CandidateScanner do BC.
function scanLines(text) {
  const found = new Set();

  for (const raw of text.split(/\r?\n/)) {
    const digits = raw.replace(/\D/g, '');
    for (const size of [47, 48]) {
      for (let i = 0; i + size <= digits.length; i++) {
        const w = digits.slice(i, i + size);
        const barcode = size === 47 ? bankSlipBarcode(w) : utilityBarcode(w);
        if (barcode) found.add(barcode);
      }
    }
  }

  return [...found];
}

// ---------- identidade da conta ----------

// O corpus nomeia por fornecedor ("12 - Energia SCS - ENEL.pdf"). O prefixo numérico
// é a ordem de pagamento do mês e MUDA — tirá-lo é o que faz o mesmo fornecedor de
// meses diferentes cair no mesmo grupo.
function accountLabel(fileName) {
  return fileName
    .replace(/\.txt$/i, '')
    .split('__').pop()
    .replace(/^\d+\s*[-_.]\s*/, '')
    .replace(/\s*\(\d+\)\s*$/, '')
    .replace(/[_]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .toUpperCase();
}

const monthOf = fileName => (fileName.match(/^(\d{4}-\d{2})/) || [, '?'])[1];

// ---------- estrutura do código de barras ----------
// Cobrança:    1-3 banco | 4 moeda | 5 DV | 6-9 fator | 10-19 valor | 20-44 campo livre
// Arrecadação: 1 produto | 2 segmento | 3 id valor | 4 DV | 5-15 valor | 16-19 empresa | 20-44 campo livre

const isUtility = b => b[0] === '8';
const freeField = b => b.slice(19);            // posições 20-44, nos dois formatos
const issuerOf = b => isUtility(b) ? b.slice(15, 19) : b.slice(0, 3);

function main() {
  const dir = process.argv[2];
  if (!dir) { console.error('uso: node analyze-account-reference.js <pasta-com-os-txt>'); process.exit(1); }

  const files = fs.readdirSync(dir).filter(f => f.toLowerCase().endsWith('.txt'));
  const docs = [];

  for (const f of files) {
    const text = fs.readFileSync(path.join(dir, f), 'utf8');
    for (const barcode of scanLines(text)) {
      docs.push({ file: f, month: monthOf(f), label: accountLabel(f), barcode });
    }
  }

  // Agrupa por (rótulo do fornecedor + emissor do código de barras): o rótulo sozinho
  // juntaria contas diferentes do mesmo fornecedor, e o emissor sozinho juntaria
  // clientes diferentes do mesmo banco.
  const groups = new Map();
  for (const d of docs) {
    const key = `${d.label} :: ${issuerOf(d.barcode)}`;
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push(d);
  }

  const recurring = [...groups.entries()]
    .map(([key, items]) => ({ key, items, months: new Set(items.map(i => i.month)) }))
    .filter(g => g.months.size >= 2)
    .sort((a, b) => b.months.size - a.months.size);

  console.log(`documentos com linha válida: ${docs.length} de ${files.length} arquivos`);
  console.log(`grupos (fornecedor × emissor): ${groups.size} — recorrentes (2+ meses): ${recurring.length}\n`);

  const summary = { stableFull: 0, stablePartial: 0, unstable: 0 };
  const rows = [];

  for (const g of recurring) {
    const barcodes = [...new Set(g.items.map(i => i.barcode))];
    const fields = barcodes.map(freeField);

    // Posição a posição: quantas das 25 do campo livre são iguais em TODOS os meses.
    let stable = 0;
    const mask = [];
    for (let i = 0; i < 25; i++) {
      const same = fields.every(f => f[i] === fields[0][i]);
      mask.push(same ? fields[0][i] : '·');
      if (same) stable++;
    }

    // O maior trecho contíguo estável é o que teria cara de "número de conta".
    const longest = mask.join('').split(/·+/).reduce((a, s) => s.length > a.length ? s : a, '');

    const kind = isUtility(barcodes[0]) ? 'arrec' : 'cobr';
    if (stable === 25) summary.stableFull++;
    else if (longest.length >= 6) summary.stablePartial++;
    else summary.unstable++;

    rows.push({
      key: g.key, kind, months: g.months.size, docs: barcodes.length,
      stable, longest: longest.length, mask: mask.join(''),
    });
  }

  console.log('grupo'.padEnd(46), 'tipo  meses docs estáveis maior  máscara do campo livre');
  for (const r of rows) {
    console.log(
      r.key.slice(0, 45).padEnd(46),
      r.kind.padEnd(5),
      String(r.months).padStart(4),
      String(r.docs).padStart(4),
      String(r.stable).padStart(7) + '/25',
      String(r.longest).padStart(5),
      ' ' + r.mask);
  }

  console.log('\n--- resumo ---');
  console.log(`campo livre INTEIRO estável ....... ${summary.stableFull}`);
  console.log(`trecho estável >= 6 dígitos ....... ${summary.stablePartial}`);
  console.log(`sem trecho estável utilizável ..... ${summary.unstable}`);
}

main();
