#!/usr/bin/env node
/**
 * SONDA DE FUMAÇA — consulta de cobrança bancária em PRODUÇÃO.
 *
 * Responde UMA pergunta que o sandbox não consegue responder:
 *
 *     em produção, o `POST /v3/bill/simulate` devolve `beneficiaryCpfCnpj`
 *     preenchido para um boleto de cobrança bancária registrado?
 *
 * Por que importa: o check `PayeeMatch` é BLOQUEANTE e compara o documento do beneficiário
 * devolvido pela consulta contra o `Payee` cadastrado. Se em produção esse campo vier vazio
 * — como veio em 100% das arrecadações medidas —, o check perde a base para cobrança
 * bancária, que é a maioria do volume, e o desenho da aprovação muda.
 *
 * O sandbox não resolve NENHUMA cobrança, nem a que ele mesmo emite (medido na sprint 1.0),
 * então esse caminho nunca foi exercido. Ver 12-official-lookup-coverage.md.
 *
 * ---------------------------------------------------------------------------
 * SEGURANÇA — leia antes de rodar
 *
 * - A chave de produção do Asaas EXIGE permissão de saque via API para atender esta consulta
 *   (achado da sprint 1.0). Ou seja: a chave que você vai usar aqui É CAPAZ DE PAGAR CONTAS.
 * - Este script chama EXCLUSIVAMENTE `/bill/simulate`, que é read-only e não move dinheiro.
 *   Não há nenhuma chamada de pagamento aqui, e não deve haver.
 * - A chave é lida só de variável de ambiente e NUNCA é impressa nem gravada.
 * - A linha digitável NUNCA é impressa: é instrumento de pagamento, quem a tem, paga.
 * - Configure a whitelist de IP no Asaas ANTES de gerar a chave (item do checklist
 *   pré-produção no CLAUDE.md).
 * - Revogue a chave assim que terminar a sonda.
 *
 * ---------------------------------------------------------------------------
 * USO
 *
 *   # PowerShell — a chave fica só na sessão, não no disco
 *   $env:ASAAS_PRODUCTION_API_KEY = "<chave de produção>"
 *   node smoke-probe-production.js "<linha digitável de 47 dígitos>" --producao
 *
 *   # ao terminar
 *   Remove-Item Env:\ASAAS_PRODUCTION_API_KEY
 */
const PRODUCTION_BASE = 'https://api.asaas.com/v3';
const ENV_KEY = 'ASAAS_PRODUCTION_API_KEY';
const CONFIRM_FLAG = '--producao';

function fail(message) {
  console.error(`\n  ERRO: ${message}\n`);
  process.exit(1);
}

function loadApiKey() {
  const key = process.env[ENV_KEY];
  if (!key || !key.trim())
    fail(`variável de ambiente ${ENV_KEY} não definida.\n`
       + `  Defina-a só na sessão do terminal — não use user-secrets para chave de produção.`);

  return key.trim();
}

/** Só dígitos. Cobrança bancária tem 47; arrecadação (48, inicia em 8) não serve aqui. */
function readDigitableLine(raw) {
  if (!raw) fail('informe a linha digitável do boleto como primeiro argumento.');

  const digits = String(raw).replace(/\D/g, '');

  if (digits.length === 48 || digits.startsWith('8'))
    fail('esta é uma linha de ARRECADAÇÃO. A sonda existe para cobrança bancária —\n'
       + '  a cobertura de arrecadação já foi medida (doc 12) e não tem lacuna em aberto.');

  if (digits.length !== 47)
    fail(`esperava 47 dígitos de cobrança bancária, recebi ${digits.length}.`);

  return digits;
}

/** Mostra só o suficiente para conferir a olho, sem publicar o documento inteiro. */
function maskTaxId(value) {
  const digits = String(value ?? '').replace(/\D/g, '');
  if (!digits) return null;
  return `${'*'.repeat(Math.max(0, digits.length - 4))}${digits.slice(-4)}`;
}

function report(label, present, detail) {
  const mark = present ? 'SIM ' : 'NAO ';
  console.log(`  [${mark}] ${label.padEnd(28)} ${detail ?? ''}`);
}

async function main() {
  const [, , lineArg, ...flags] = process.argv;

  if (!flags.includes(CONFIRM_FLAG))
    fail(`esta sonda usa a chave de PRODUÇÃO, que é capaz de pagar contas.\n`
       + `  Se é isso mesmo, repita o comando com ${CONFIRM_FLAG} no fim.`);

  // A linha é conferida antes da chave: dá para validar que o boleto serve para a sonda
  // sem precisar ter a credencial de produção em mãos.
  const identificationField = readDigitableLine(lineArg);
  const apiKey = loadApiKey();

  console.log('\n  Sonda de fumaça — POST /v3/bill/simulate em PRODUÇÃO (read-only)\n');

  const started = Date.now();
  const res = await fetch(`${PRODUCTION_BASE}/bill/simulate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', access_token: apiKey },
    body: JSON.stringify({ identificationField }),
  });

  let body = null;
  try { body = await res.json(); } catch { /* resposta sem corpo JSON */ }

  console.log(`  HTTP ${res.status} em ${Date.now() - started}ms\n`);

  if (!res.ok) {
    const err = body?.errors?.[0];
    console.log(`  A consulta NÃO resolveu: ${err?.code ?? 'sem código'} — ${err?.description ?? 'sem descrição'}\n`);

    if (err?.code === 'insufficient_permission')
      console.log('  >> A chave não tem permissão de saque via API. É pré-requisito da consulta.\n');
    else if (err?.code === 'unregistered_bank_slip')
      console.log('  >> Boleto não registrado na rede bancária. Use um boleto RECENTE e NÃO PAGO\n'
                + '     de um emissor grande (banco de primeira linha), e tente de novo.\n');

    process.exit(2);
  }

  const info = body?.bankSlipInfo ?? {};
  const hasTaxId = Boolean(String(info.beneficiaryCpfCnpj ?? '').replace(/\D/g, ''));

  console.log('  Campos que os checks consomem:\n');
  report('beneficiaryCpfCnpj', hasTaxId, maskTaxId(info.beneficiaryCpfCnpj) ?? '(vazio)');
  report('beneficiaryName', Boolean(info.beneficiaryName), info.beneficiaryName ?? '(vazio)');
  report('companyName', Boolean(info.companyName), info.companyName ?? '(vazio)');
  report('bank', Boolean(info.bank), JSON.stringify(info.bank ?? null));
  report('value', info.value != null, String(info.value ?? '(vazio)'));
  report('originalValue', info.originalValue != null, String(info.originalValue ?? '(vazio)'));
  report('dueDate', Boolean(info.dueDate), info.dueDate ?? '(vazio)');
  report('isOverdue', info.isOverdue != null, String(info.isOverdue ?? '(vazio)'));
  report('allowChangeValue', info.allowChangeValue != null, String(info.allowChangeValue ?? '(vazio)'));
  report('minimumScheduleDate', Boolean(body?.minimumScheduleDate), body?.minimumScheduleDate ?? '(vazio)');

  console.log('\n  VEREDITO\n');

  if (hasTaxId) {
    console.log('  SONDA VERDE. Produção devolve o documento do beneficiário para cobrança bancária.\n'
              + '  O check PayeeMatch bloqueante tem base, e a tela pode prometer "beneficiário\n'
              + '  verificado" para este tipo de documento. Marque o item no checklist do CLAUDE.md\n'
              + '  e atualize a tabela de Resultado do doc 12.\n');
    return;
  }

  console.log('  SONDA VERMELHA. Produção resolveu o título mas NÃO devolveu o documento do\n'
            + '  beneficiário. Consequência: PayeeMatch degrada para cotejo por NOME também em\n'
            + '  cobrança bancária — a mesma verificação parcial da arrecadação. Isso muda o\n'
            + '  desenho da aprovação (doc 03, check 5) e a promessa da interface.\n'
            + '  NÃO trate como detalhe: registre no doc 12 e reveja o doc 03 antes de seguir.\n');
  process.exit(3);
}

main().catch((error) => fail(error.message));
