#!/usr/bin/env node
/**
 * Sprint 1.0 — mede a cobertura do `POST /v3/bill/simulate` do Asaas sobre o corpus real,
 * separando cobrança bancária de arrecadação (DARF, DAS, SABESP, DAE, EDP).
 *
 * A pergunta que este script responde: a consulta oficial cobre arrecadação? Se não cobrir,
 * ~45% do volume real fica sem check de beneficiário e o plano B do ADR-001 precisa entrar.
 *
 * A chave NUNCA é impressa nem gravada. É lida de:
 *   1. ASAAS_SANDBOX_API_KEY (variável de ambiente), ou
 *   2. dotnet user-secrets do BillPayment.API (Asaas:SandboxApiKey)
 *
 * Uso:
 *   node analyze-boleto-corpus.js <pasta-txt> --json > lines.json
 *   node probe-asaas-simulate.js lines.json
 */
const fs = require('fs');
const os = require('os');
const path = require('path');

const USER_SECRETS_ID = '0e91935b-cdef-4708-9f02-813694df3493';
const SANDBOX_BASE = 'https://api-sandbox.asaas.com/v3';
// O mesmo valor que a API manda em produção (AsaasOptions.USER_AGENT). O provedor exige o
// cabeçalho, e o fetch do Node preencheria `node` sozinho — o que fez esta sonda passar em
// 2026-08-06 contra um endpoint que o adapter .NET não conseguia chamar.
const USER_AGENT = 'RufinoBillPayment/1.0';

const SECRET_KEY = 'Asaas:SandboxApiKey';

function loadApiKey() {
  if (process.env.ASAAS_SANDBOX_API_KEY) return process.env.ASAAS_SANDBOX_API_KEY.trim();

  const appData = process.env.APPDATA
    || path.join(os.homedir(), '.microsoft', 'usersecrets');
  const secretsPath = process.env.APPDATA
    ? path.join(appData, 'Microsoft', 'UserSecrets', USER_SECRETS_ID, 'secrets.json')
    : path.join(appData, USER_SECRETS_ID, 'secrets.json');

  if (!fs.existsSync(secretsPath))
    throw new Error(`Chave não encontrada. Defina ASAAS_SANDBOX_API_KEY ou rode:\n  dotnet user-secrets set "${SECRET_KEY}" "<chave>" --project BillPayment.API`);

  // `dotnet user-secrets` grava com BOM no Windows e JSON.parse engasga nele.
  const secrets = JSON.parse(fs.readFileSync(secretsPath, 'utf8').replace(/^﻿/, ''));
  const key = secrets[SECRET_KEY] ?? secrets.Asaas?.SandboxApiKey;
  if (!key) throw new Error(`"${SECRET_KEY}" ausente em ${secretsPath}`);
  return String(key).trim();
}

async function simulate(apiKey, identificationField) {
  const started = Date.now();
  const res = await fetch(`${SANDBOX_BASE}/bill/simulate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', access_token: apiKey, 'User-Agent': USER_AGENT },
    body: JSON.stringify({ identificationField }),
  });

  let body = null;
  try { body = await res.json(); } catch { /* resposta sem corpo JSON */ }

  return { status: res.status, ok: res.ok, body, elapsedMs: Date.now() - started };
}

// Um campo por verificação do doc 03. `companyName` entra porque, em arrecadação, é o
// único identificador do beneficiário que volta — a primeira versão desta sonda não o
// media e concluiu "sem beneficiário" quando o que faltava era só o CNPJ.
const FIELDS_THAT_MATTER = [
  'beneficiaryCpfCnpj', 'beneficiaryName', 'companyName', 'bank',
  'value', 'originalValue', 'dueDate', 'isOverdue', 'allowChangeValue',
];

function describe(result) {
  if (!result.ok) {
    const err = result.body?.errors?.[0];
    return { covered: false, detail: err ? `${err.code ?? ''} ${err.description ?? ''}`.trim() : `HTTP ${result.status}` };
  }

  const info = result.body?.bankSlipInfo ?? {};
  const has = f => info[f] !== undefined && info[f] !== null;

  return {
    covered: true,
    fields: FIELDS_THAT_MATTER.filter(has),
    byTaxId: has('beneficiaryCpfCnpj'),
    byName: has('beneficiaryName') || has('companyName'),
    detail: [
      has('beneficiaryCpfCnpj') ? 'doc' : 'SEM doc',
      has('beneficiaryName') || has('companyName') ? 'nome' : 'SEM nome',
      has('bank') ? 'banco' : 'SEM banco',
      has('value') ? 'valor' : 'SEM valor',
      has('dueDate') ? 'venc' : 'SEM venc',
    ].join(' · '),
  };
}

(async () => {
  const inputPath = process.argv[2];
  if (!inputPath) {
    console.error('uso: node probe-asaas-simulate.js <lines.json>');
    process.exit(1);
  }

  const apiKey = loadApiKey();
  const lines = JSON.parse(fs.readFileSync(inputPath, 'utf8'));
  const blank = () => ({ total: 0, covered: 0, byTaxId: 0, byName: 0, fields: {} });
  const stats = { cobranca: blank(), arrecadacao: blank() };

  for (const item of lines) {
    const bucket = stats[item.kind];
    bucket.total++;

    let outcome;
    try {
      outcome = describe(await simulate(apiKey, item.digitableLine));
    } catch (e) {
      outcome = { covered: false, detail: `falha de rede: ${e.message}` };
    }

    if (outcome.covered) {
      bucket.covered++;
      if (outcome.byTaxId) bucket.byTaxId++;
      if (outcome.byName) bucket.byName++;
      for (const f of outcome.fields) bucket.fields[f] = (bucket.fields[f] ?? 0) + 1;
    }

    const flag = outcome.covered ? (outcome.byTaxId ? 'OK   ' : 'PARC ') : 'FALHA';
    console.log(`${flag} ${item.kind.padEnd(12)} ${item.file.padEnd(44)} ${outcome.detail}`);

    // Cadência humana: o sandbox é compartilhado e não há pressa nesta investigação.
    await new Promise(r => setTimeout(r, 400));
  }

  console.log('\n=== COBERTURA ===');
  for (const [kind, s] of Object.entries(stats)) {
    if (s.total === 0) continue;
    const pct = n => `${((n / s.total) * 100).toFixed(0)}%`;
    console.log(`\n${kind}  consultados=${s.total}  responderam=${s.covered} (${pct(s.covered)})`);
    console.log(`  beneficiário por documento .. ${s.byTaxId} (${pct(s.byTaxId)})`);
    console.log(`  beneficiário por nome ....... ${s.byName} (${pct(s.byName)})`);
    for (const f of FIELDS_THAT_MATTER)
      console.log(`  ${f.padEnd(28, '.')} ${s.fields[f] ?? 0} (${pct(s.fields[f] ?? 0)})`);
  }

  console.log(`
Leitura: "beneficiário por documento" é o que sustenta o check forte do doc 03.
Caindo para "por nome", o check passa a depender de Payee.LegalName + Aliases.
`);
})();
