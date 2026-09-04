#!/usr/bin/env node
/**
 * Atualiza o snapshot da tabela de instituições do Banco Central usado pelo BankDirectory.
 *
 * Fonte: relação de participantes do STR, publicada pelo Bacen no Portal de Dados Abertos.
 *   https://dadosabertos.bcb.gov.br/dataset/lista-de-participantes-do-str
 *
 * Por que snapshot e não consulta ao vivo: a tabela decide um check que autoriza pagamento.
 * Depender do bcb.gov.br estar no ar em tempo de validação transformaria indisponibilidade
 * externa em bloqueio de pagamento. A tabela muda algumas vezes por ano; snapshot versionado
 * é auditável e determinístico. Rode este script quando quiser atualizar, revise o diff e
 * commite — a mudança fica visível no histórico, que é o ponto.
 *
 * Uso:
 *   node fetch-bacen-participants.js
 *   node fetch-bacen-participants.js --out ../../BillPayment.Infra/BankDirectory/bacen-participants.csv
 */
const fs = require('fs');
const path = require('path');

// A URL do Portal de Dados Abertos (content/estabilidadefinanceira/str1/) responde 404;
// a que serve o arquivo de verdade é esta. Se ela cair, comece pelo dataset do portal.
const SOURCE = 'https://www.bcb.gov.br/pom/spb/estatistica/port/ParticipantesSTRport.csv';

const DEFAULT_OUT = path.join(
  __dirname, '..', '..', 'BillPayment.Infra', 'BankDirectory', 'bacen-participants.csv');

function parseCsv(text) {
  const [header, ...lines] = text.replace(/^﻿/, '').trim().split(/\r?\n/);
  const columns = header.split(',');

  return lines.map(line => {
    // O Nome_Extenso pode conter vírgula entre aspas; as colunas que nos interessam
    // vêm antes dele, então um split simples basta para as quatro primeiras.
    const cells = line.split(',');
    return Object.fromEntries(columns.map((c, i) => [c, (cells[i] ?? '').trim()]));
  });
}

(async () => {
  const outArg = process.argv.indexOf('--out');
  const outPath = outArg !== -1 ? process.argv[outArg + 1] : DEFAULT_OUT;

  const res = await fetch(SOURCE);
  if (!res.ok) throw new Error(`Bacen respondeu ${res.status} para ${SOURCE}`);

  const rows = parseCsv(await res.text());
  const [ispbCol, nameCol, codeCol, compeCol] = Object.keys(rows[0]);

  const kept = rows
    .filter(r => /^\d{3}$/.test(r[codeCol]))          // sem código de três dígitos não serve a boleto
    .map(r => ({
      ispb: r[ispbCol].padStart(8, '0'),
      compe: r[codeCol],
      participatesInCompe: r[compeCol] === 'Sim' ? '1' : '0',
      name: r[nameCol].replace(/[,"]/g, ' ').replace(/\s+/g, ' ').trim(),
    }))
    .sort((a, b) => a.compe.localeCompare(b.compe));

  const duplicates = kept.map(r => r.compe).filter((c, i, all) => all.indexOf(c) !== i);
  if (duplicates.length)
    throw new Error(`código COMPE duplicado na fonte: ${[...new Set(duplicates)].join(', ')}`);

  const body = ['ispb,compe,participates_in_compe,name',
    ...kept.map(r => `${r.ispb},${r.compe},${r.participatesInCompe},${r.name}`)].join('\n');

  fs.mkdirSync(path.dirname(outPath), { recursive: true });
  fs.writeFileSync(outPath, `${body}\n`, 'utf8');

  const inCompe = kept.filter(r => r.participatesInCompe === '1').length;
  console.log(`${kept.length} instituições gravadas em ${outPath}`);
  console.log(`  participam da Compe: ${inCompe}`);
  console.log(`  fonte: ${SOURCE}`);
  console.log('\nRevise o diff antes de commitar — esta tabela decide um check de pagamento.');
})();
