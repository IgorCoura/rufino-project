#!/usr/bin/env node
/**
 * SONDA DE FUMAÇA — decode de QR Pix em PRODUÇÃO.
 *
 * Irmã de `smoke-probe-production.js`, para a lacuna que sobrou: o
 * `POST /v3/pix/qrCodes/decode` **nunca teve resposta de sucesso observada**, em ambiente
 * nenhum. O que foi medido na sprint 1.0 foi só o 403 `insufficient_permission` sem a
 * permissão de saque — nada sobre o corpo que ele devolve quando funciona.
 *
 * Isso importa mais do que parece: o ADR-010 faz do Pix o trilho PREFERENCIAL, e o check
 * `PixBarcodeConsistency` — a defesa contra QR adulterado colado sobre boleto verdadeiro —
 * compara as duas consultas. Uma delas nunca foi vista respondendo.
 *
 * O mapeamento de `AsaasPixLookupService` veio da DOCUMENTAÇÃO do provedor, não de medição.
 * Os testes de unidade provam que a tradução está certa *dado aquele contrato*; não provam
 * que o contrato é o real.
 *
 * ---------------------------------------------------------------------------
 * SEGURANÇA
 *
 * - Chama EXCLUSIVAMENTE `/pix/qrCodes/decode`, que é read-only. NÃO existe chamada de
 *   pagamento aqui — `/pix/qrCodes/pay` não aparece neste arquivo e não deve aparecer.
 * - A chave exige permissão de saque (mesmo pré-requisito do simulate) e portanto É CAPAZ DE
 *   PAGAR CONTAS. Whitelist de IP antes; revogação depois.
 * - Chave e payload NUNCA são impressos.
 *
 * ---------------------------------------------------------------------------
 * USO
 *
 *   $env:ASAAS_PRODUCTION_API_KEY = "<chave>"
 *   node smoke-probe-pix-decode.js "<payload do BR Code>" --producao
 *   Remove-Item Env:\ASAAS_PRODUCTION_API_KEY
 *
 * Onde conseguir o payload: é o texto que o app do banco mostra em "Pix Copia e Cola".
 * Num boleto híbrido ele costuma estar só como IMAGEM de QR — leia com o celular e copie.
 */
const PRODUCTION_BASE = 'https://api.asaas.com/v3';
const ENV_KEY = 'ASAAS_PRODUCTION_API_KEY';
const CONFIRM_FLAG = '--producao';

function fail(message) {
  console.error(`\n  ERRO: ${message}\n`);
  process.exit(1);
}

/** CRC-16/CCITT-FALSE — mesma implementação do domínio, para recusar payload truncado. */
function crc16(text) {
  let crc = 0xffff;
  for (const ch of Buffer.from(text, 'utf8')) {
    crc ^= ch << 8;
    for (let i = 0; i < 8; i++) crc = crc & 0x8000 ? ((crc << 1) ^ 0x1021) & 0xffff : (crc << 1) & 0xffff;
  }
  return crc.toString(16).toUpperCase().padStart(4, '0');
}

function readPayload(raw) {
  if (!raw) fail('informe o payload do BR Code (Pix Copia e Cola) como primeiro argumento.');

  const payload = String(raw).trim();
  if (!payload.startsWith('000201'))
    fail('não parece um BR Code: todo payload EMV começa com "000201".');

  const marker = payload.lastIndexOf('6304');
  if (marker < 0 || marker + 8 !== payload.length)
    fail('o payload não termina no campo de CRC ("6304" + 4 caracteres). Copiou pela metade?');

  const expected = crc16(payload.slice(0, marker + 4));
  const actual = payload.slice(marker + 4).toUpperCase();
  if (expected !== actual)
    fail(`CRC inválido (esperado ${expected}, veio ${actual}). O payload foi copiado incompleto ou alterado.`);

  return payload;
}

function report(label, present, detail) {
  console.log(`  [${present ? 'SIM ' : 'NAO '}] ${label.padEnd(30)} ${detail ?? ''}`);
}

function maskTaxId(value) {
  const digits = String(value ?? '').replace(/\D/g, '');
  if (!digits) return null;
  return `${'*'.repeat(Math.max(0, digits.length - 4))}${digits.slice(-4)}`;
}

async function main() {
  const [, , payloadArg, ...flags] = process.argv;

  if (!flags.includes(CONFIRM_FLAG))
    fail(`esta sonda usa a chave de PRODUÇÃO, que é capaz de pagar contas.\n`
       + `  Se é isso mesmo, repita o comando com ${CONFIRM_FLAG} no fim.`);

  const payload = readPayload(payloadArg);

  const apiKey = process.env[ENV_KEY]?.trim();
  if (!apiKey) fail(`variável de ambiente ${ENV_KEY} não definida.`);

  console.log('\n  Sonda de fumaça — POST /v3/pix/qrCodes/decode em PRODUÇÃO (read-only)');
  console.log('  CRC do payload conferido localmente antes de enviar.\n');

  const started = Date.now();
  const res = await fetch(`${PRODUCTION_BASE}/pix/qrCodes/decode`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', access_token: apiKey },
    body: JSON.stringify({ payload }),
  });

  let body = null;
  try { body = await res.json(); } catch { /* sem corpo JSON */ }

  console.log(`  HTTP ${res.status} em ${Date.now() - started}ms\n`);

  if (!res.ok) {
    const err = body?.errors?.[0];
    console.log(`  NÃO decodificou: ${err?.code ?? 'sem código'} — ${err?.description ?? 'sem descrição'}\n`);
    if (err?.code === 'insufficient_permission')
      console.log('  >> A chave não tem permissão de saque via API. É pré-requisito do decode.\n');
    process.exit(2);
  }

  const receiver = body?.receiver ?? {};
  const payer = body?.payer ?? {};
  const hasReceiverTaxId = Boolean(String(receiver.cpfCnpj ?? '').replace(/\D/g, ''));

  console.log('  Campos que os checks consomem:\n');
  report('receiver.cpfCnpj', hasReceiverTaxId, maskTaxId(receiver.cpfCnpj) ?? '(vazio)');
  report('receiver.name', Boolean(receiver.name), receiver.name ?? '(vazio)');
  report('receiver.tradingName', Boolean(receiver.tradingName), receiver.tradingName ?? '(vazio)');
  report('receiver.ispb', Boolean(receiver.ispb), receiver.ispb ?? '(vazio)');
  report('receiver.ispbName', Boolean(receiver.ispbName), receiver.ispbName ?? '(vazio)');
  report('receiver.personType', Boolean(receiver.personType), receiver.personType ?? '(vazio)');
  report('type (STATIC/DYNAMIC)', Boolean(body?.type), body?.type ?? '(vazio)');
  report('value', body?.value != null, String(body?.value ?? '(vazio)'));
  report('totalValue', body?.totalValue != null, String(body?.totalValue ?? '(vazio)'));
  report('interest / fine / discount', body?.interest != null || body?.fine != null || body?.discount != null,
    `${body?.interest ?? '-'} / ${body?.fine ?? '-'} / ${body?.discount ?? '-'}`);
  report('dueDate', Boolean(body?.dueDate), body?.dueDate ?? '(vazio)');
  report('expirationDate', Boolean(body?.expirationDate), body?.expirationDate ?? '(vazio)');
  report('canBePaid', body?.canBePaid != null, String(body?.canBePaid ?? '(vazio)'));
  report('canBePaidWithDifferentValue', body?.canBePaidWithDifferentValue != null,
    String(body?.canBePaidWithDifferentValue ?? '(vazio)'));
  report('conciliationIdentifier', Boolean(body?.conciliationIdentifier), body?.conciliationIdentifier ?? '(vazio)');
  report('payer.cpfCnpj (mascarado)', Boolean(payer.cpfCnpj), payer.cpfCnpj ?? '(vazio)');

  // O que o adapter mapeia e o provedor não devolveu: candidato a campo inventado pela doc.
  const expected = ['type', 'receiver', 'value', 'totalValue', 'canBePaid', 'canBePaidWithDifferentValue'];
  const missing = expected.filter((k) => body?.[k] === undefined);
  // Lista do que o adapter JÁ mapeia. Medida contra produção em 2026-08-06 — antes disso
  // ela vinha da documentação e produzia falso positivo (acusava `cannotBePaidReason` de
  // não mapeado quando ele estava no DTO desde a 1.3).
  const mapped = [...expected, 'payer', 'interest', 'fine', 'discount', 'dueDate', 'expirationDate',
    'conciliationIdentifier', 'cannotBePaidReason', 'description', 'payload', 'changeValue',
    'endToEndIdentifier', 'transactionIdentifier'];
  const unknown = Object.keys(body ?? {}).filter((k) => !mapped.includes(k));

  console.log('\n  ADERÊNCIA AO CONTRATO ASSUMIDO PELO ADAPTER\n');
  console.log(`  campos esperados ausentes .. ${missing.length ? missing.join(', ') : 'nenhum'}`);
  console.log(`  campos novos não mapeados .. ${unknown.length ? unknown.join(', ') : 'nenhum'}`);

  console.log('\n  VEREDITO\n');
  if (hasReceiverTaxId) {
    console.log('  SONDA VERDE. O decode devolve o documento do recebedor — que é a única fonte\n'
              + '  dele no trilho Pix, já que o BR Code carrega só chave e nome. PayeeMatch e\n'
              + '  PixBarcodeConsistency têm base no trilho preferencial.\n');
    if (missing.length || unknown.length)
      console.log('  ATENÇÃO: o contrato real diverge do assumido (ver acima). Ajuste\n'
                + '  AsaasPixLookupService/AsaasContracts e o doc 04 antes de confiar no mapeamento.\n');
    return;
  }

  console.log('  SONDA VERMELHA. O decode resolveu mas NÃO devolveu o documento do recebedor.\n'
            + '  Consequência: o trilho Pix fica sem check forte de beneficiário, e o ADR-010\n'
            + '  (Pix preferido sobre boleto) precisa ser reaberto — preferir um trilho que\n'
            + '  verifica menos inverteria a lógica inteira.\n');
  process.exit(3);
}

main().catch((error) => fail(error.message));
