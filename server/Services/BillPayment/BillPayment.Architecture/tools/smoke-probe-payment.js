#!/usr/bin/env node
/**
 * Sprint 3.0 — mede, em SANDBOX, o que a fase 3 precisa saber antes de escrever o gateway
 * de pagamento. O critério de pronto da fase depende do que este script responde:
 *
 *   1. O sandbox processa `POST /v3/bill` sobre um boleto emitido por ele mesmo?
 *      (A medição da 1.0 mostrou que o `bill/simulate` NÃO resolve cobrança em sandbox —
 *      nem a que o próprio sandbox emite. Se o pagamento herdar essa limitação, o critério
 *      de pronto "Pending → BankProcessing → Paid em sandbox" é inexequível no trilho boleto.)
 *   2. A busca por `externalReference` encontra a ordem? (É a idempotência de submissão:
 *      timeout na criação NÃO pode gerar dois pagamentos.)
 *   3. `GET /v3/bill/{id}` devolve `transactionReceiptUrl`? A URL exige autenticação ou é
 *      capability URL? Serve PDF ou página? (Decide a captura do comprovante da 3.3.)
 *   4. O trilho Pix fecha em sandbox? (decode do QR de uma cobrança própria → pay com
 *      `scheduleDate` → cancelamento.)
 *   5. Qual é o contrato real do `GET /v3/webhooks`? (A 3.3 provisiona webhook por conta
 *      do tenant; o payload é medido, não lido — regra do BC.)
 *   6. Como o provedor reage a saldo insuficiente? (Sandbox costuma nascer com saldo zero,
 *      o que por si já é medição da 3.2.)
 *
 * SÓ SANDBOX, POR CONSTRUÇÃO. A base é constante e não há flag de produção de propósito:
 * este script cria clientes, cobranças e ordens de pagamento — nada disso pode encostar em
 * conta real. A sonda de produção da fase 3, se um dia existir, é outro arquivo e outra
 * decisão explícita do usuário.
 *
 * A chave NUNCA é impressa nem gravada. É lida de:
 *   1. ASAAS_SANDBOX_API_KEY (variável de ambiente), ou
 *   2. dotnet user-secrets do BillPayment.API (Asaas:SandboxApiKey, com fallback para
 *      Asaas:ApiKey — o nome usado no incidente dos UnconfiguredLookupTests, sabidamente
 *      de sandbox).
 *
 * Uso:
 *   node smoke-probe-payment.js
 */
const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');

const USER_SECRETS_ID = '0e91935b-cdef-4708-9f02-813694df3493';
const SANDBOX_BASE = 'https://api-sandbox.asaas.com/v3';
// O mesmo valor que a API manda (AsaasOptions.USER_AGENT). O provedor exige o cabeçalho e o
// fetch do Node preencheria `node` sozinho — a lição do gotchas: a sonda tem que mandar o que
// o .NET manda, senão mede outra coisa.
const USER_AGENT = 'RufinoBillPayment/1.0';

function loadApiKey() {
  if (process.env.ASAAS_SANDBOX_API_KEY) return process.env.ASAAS_SANDBOX_API_KEY.trim();

  const appData = process.env.APPDATA || path.join(os.homedir(), '.microsoft', 'usersecrets');
  const secretsPath = process.env.APPDATA
    ? path.join(appData, 'Microsoft', 'UserSecrets', USER_SECRETS_ID, 'secrets.json')
    : path.join(appData, USER_SECRETS_ID, 'secrets.json');

  if (!fs.existsSync(secretsPath))
    throw new Error('Chave não encontrada. Defina ASAAS_SANDBOX_API_KEY ou grave Asaas:SandboxApiKey no user-secrets do BillPayment.API.');

  const secrets = JSON.parse(fs.readFileSync(secretsPath, 'utf8').replace(/^﻿/, ''));
  const key = secrets['Asaas:SandboxApiKey'] ?? secrets['Asaas:ApiKey']
    ?? secrets.Asaas?.SandboxApiKey ?? secrets.Asaas?.ApiKey;
  if (!key) throw new Error('Nenhuma chave de sandbox no user-secrets (Asaas:SandboxApiKey / Asaas:ApiKey).');
  return String(key).trim();
}

async function call(apiKey, method, pathname, body) {
  const started = Date.now();
  const res = await fetch(`${SANDBOX_BASE}${pathname}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      access_token: apiKey,
      'User-Agent': USER_AGENT,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  let json = null;
  try { json = await res.json(); } catch { /* sem corpo JSON */ }
  return { status: res.status, ok: res.ok, body: json, elapsedMs: Date.now() - started };
}

function log(title, r) {
  const err = r.body?.errors?.map(e => `${e.code ?? ''} ${e.description ?? ''}`.trim()).join(' | ');
  console.log(`\n== ${title}`);
  console.log(`   HTTP ${r.status} em ${r.elapsedMs}ms${err ? ` — ${err}` : ''}`);
  return r;
}

function pick(obj, fields) {
  const out = {};
  for (const f of fields) if (obj && obj[f] !== undefined) out[f] = obj[f];
  return out;
}

const iso = d => d.toISOString().slice(0, 10);
const plusDays = n => { const d = new Date(); d.setDate(d.getDate() + n); return d; };

async function main() {
  const apiKey = loadApiKey();
  const runTag = crypto.randomUUID();
  console.log(`Sonda de pagamento — SANDBOX (${SANDBOX_BASE})`);
  console.log(`externalReference desta rodada: ${runTag}`);

  // 0. Saldo — prova a chave e mede o ponto de partida da 3.2.
  const balance = log('Saldo (GET /finance/balance)', await call(apiKey, 'GET', '/finance/balance'));
  if (balance.ok) console.log('  ', JSON.stringify(balance.body));
  if (!balance.ok) { console.log('Chave recusada — nada mais a medir.'); return; }

  // 1. Cliente + cobrança BOLETO emitida pelo próprio sandbox: é o único boleto que o
  //    sandbox tem chance de conhecer.
  const customer = log('Cliente de sonda (POST /customers)', await call(apiKey, 'POST', '/customers', {
    name: 'Sonda Fase 3 (descartavel)',
    cpfCnpj: '52998224725',
  }));
  if (!customer.ok) return;

  const charge = log('Cobrança BOLETO (POST /payments)', await call(apiKey, 'POST', '/payments', {
    customer: customer.body.id,
    billingType: 'BOLETO',
    value: 5.55,
    dueDate: iso(plusDays(7)),
    description: 'sonda fase 3 — boleto',
  }));
  if (!charge.ok) return;

  const line = log('Linha digitável (GET /payments/{id}/identificationField)',
    await call(apiKey, 'GET', `/payments/${charge.body.id}/identificationField`));
  const identificationField = line.body?.identificationField;
  console.log(`   linha: ${identificationField ? identificationField.length + ' dígitos' : 'AUSENTE'}`);
  if (!identificationField) return;

  // 2. O simulate ainda falha sobre boleto do próprio sandbox? (referência da 1.0)
  const sim = log('Consulta (POST /bill/simulate)', await call(apiKey, 'POST', '/bill/simulate', { identificationField }));
  if (sim.ok) console.log('  ', JSON.stringify(pick(sim.body?.bankSlipInfo ?? {}, ['beneficiaryCpfCnpj', 'beneficiaryName', 'value', 'dueDate', 'bank'])), JSON.stringify(pick(sim.body ?? {}, ['minimumScheduleDate', 'fee'])));

  // 3. A pergunta central: o sandbox aceita pagar/agendar este boleto?
  const bill = log('Pagamento agendado (POST /bill)', await call(apiKey, 'POST', '/bill', {
    identificationField,
    scheduleDate: iso(plusDays(2)),
    value: 5.55,
    dueDate: iso(plusDays(7)),
    description: 'sonda fase 3 — agendamento',
    externalReference: runTag,
  }));
  if (bill.ok) console.log('  ', JSON.stringify(pick(bill.body, ['id', 'status', 'scheduleDate', 'value', 'fee', 'canBeCancelled', 'externalReference', 'transactionReceiptUrl'])));

  // 4. Idempotência: a busca por externalReference encontra o que acabou de ser criado?
  const search = log('Busca por externalReference (GET /bill?externalReference=…)',
    await call(apiKey, 'GET', `/bill?externalReference=${runTag}`));
  if (search.ok) console.log(`   totalCount=${search.body?.totalCount} — ids: ${(search.body?.data ?? []).map(b => b.id).join(', ') || '(nenhum)'}`);

  if (bill.ok) {
    // 5. O retrato da ordem — e o campo do comprovante.
    const got = log('Consulta da ordem (GET /bill/{id})', await call(apiKey, 'GET', `/bill/${bill.body.id}`));
    if (got.ok) {
      console.log('  ', JSON.stringify(pick(got.body, ['status', 'scheduleDate', 'paymentDate', 'transactionReceiptUrl', 'canBeCancelled', 'failReasons'])));
      if (got.body.transactionReceiptUrl) {
        // Mede a natureza da URL do comprovante SEM credencial — capability URL ou autenticada?
        const rec = await fetch(got.body.transactionReceiptUrl, { headers: { 'User-Agent': USER_AGENT }, redirect: 'manual' });
        console.log(`   comprovante sem auth: HTTP ${rec.status}, content-type=${rec.headers.get('content-type')}`);
      } else {
        console.log('   transactionReceiptUrl ainda nulo (esperado antes de PAID).');
      }
    }

    // 6. Cancelamento — a janela de reação que a política das 24h existe para garantir.
    const cancel = log('Cancelamento (POST /bill/{id}/cancel)', await call(apiKey, 'POST', `/bill/${bill.body.id}/cancel`, {}));
    if (cancel.ok) console.log('  ', JSON.stringify(pick(cancel.body, ['id', 'status'])));
  }

  // 7. Trilho Pix: cobrança PIX própria → payload → decode → pay agendado → cancel.
  const pixCharge = log('Cobrança PIX (POST /payments)', await call(apiKey, 'POST', '/payments', {
    customer: customer.body.id,
    billingType: 'PIX',
    value: 4.44,
    dueDate: iso(plusDays(7)),
    description: 'sonda fase 3 — pix',
  }));
  if (pixCharge.ok) {
    const qr = log('Payload do QR (GET /payments/{id}/pixQrCode)', await call(apiKey, 'GET', `/payments/${pixCharge.body.id}/pixQrCode`));
    const payload = qr.body?.payload;
    console.log(`   payload: ${payload ? payload.length + ' chars' : 'AUSENTE'}`);

    if (payload) {
      const decode = log('Decode (POST /pix/qrCodes/decode)', await call(apiKey, 'POST', '/pix/qrCodes/decode', { payload }));
      if (decode.ok) console.log('  ', JSON.stringify(pick(decode.body, ['type', 'value', 'totalValue', 'canBePaid', 'cannotBePaidReason', 'dueDate', 'expirationDate'])));

      const pay = log('Pagamento Pix agendado (POST /pix/qrCodes/pay)', await call(apiKey, 'POST', '/pix/qrCodes/pay', {
        qrCode: { payload },
        value: decode.body?.value ?? 4.44,
        description: 'sonda fase 3 — pix agendado',
        scheduleDate: iso(plusDays(2)),
        externalReference: runTag,
      }));
      if (pay.ok) {
        console.log('  ', JSON.stringify(pick(pay.body, ['id', 'status', 'scheduledDate', 'endToEndIdentifier', 'canBeCanceled', 'canBeRefunded', 'externalReference', 'transactionReceiptUrl'])));
        const pixCancel = log('Cancelamento Pix (POST /pix/transactions/{id}/cancel)',
          await call(apiKey, 'POST', `/pix/transactions/${pay.body.id}/cancel`, {}));
        if (pixCancel.ok) console.log('  ', JSON.stringify(pick(pixCancel.body, ['id', 'status'])));
      }
    }
  }

  // 8. O contrato real de webhooks — a 3.3 provisiona por conta do tenant.
  const hooks = log('Webhooks configurados (GET /webhooks)', await call(apiKey, 'GET', '/webhooks'));
  if (hooks.ok) console.log('  ', JSON.stringify(hooks.body).slice(0, 400));

  console.log('\nFim. Nada do que a sonda criou é real: sandbox.');
}

main().catch(e => { console.error(`Falha da sonda: ${e.message}`); process.exit(1); });
