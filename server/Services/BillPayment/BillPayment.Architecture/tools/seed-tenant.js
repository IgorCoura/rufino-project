#!/usr/bin/env node
/**
 * CADASTRO DE UM TENANT, a partir de um arquivo — repetível e idempotente.
 *
 * Resolve um problema concreto: o desfecho de um artefato capturado depende do cadastro que
 * existia quando ele passou. Sem `PayerProfile` não há senha derivada; sem `Payee` nem
 * `TrustedOrigin`, o que a cascata não reconhece é DESCARTADO em vez de ir para a quarentena.
 * Refazer isso à mão a cada ambiente novo é como o cadastro diverge — e cadastro divergente
 * produz medição que não se compara com a anterior.
 *
 * ---------------------------------------------------------------------------
 * POR QUE PELA API, E NÃO POR SQL
 *
 * Cada linha entra pelo mesmo caminho que a tela usa, então passa por TaxId.Parse, pelo dígito
 * verificador, pela normalização de e-mail e por toda invariante do agregado. Um seed por SQL
 * conseguiria gravar um CNPJ inválido — e o defeito só apareceria meses depois, na consulta
 * oficial de um boleto de verdade.
 *
 * ---------------------------------------------------------------------------
 * IDEMPOTENTE
 *
 * Rodar duas vezes não duplica: `409 Conflict` (já cadastrado) é contado como "já existia" e
 * segue adiante. Então o arquivo é a fonte da verdade — acrescente uma linha nele e rode de novo.
 *
 * ---------------------------------------------------------------------------
 * O ARQUIVO NÃO É VERSIONADO, E ISSO É DELIBERADO
 *
 * Ele contém CNPJ e CPF reais — dado de pessoa e de empresa, que não entra no repositório
 * (`*.local.json` está no .gitignore). O que é versionado é o `.example`, com a forma.
 * Guarde o seu num backup junto com a master key do cofre.
 *
 * ---------------------------------------------------------------------------
 * USO
 *
 *   cp seed-tenant.example.json seed-tenant.local.json   # e edite
 *   node seed-tenant.js --api=http://localhost:8100
 *   node seed-tenant.js --api=http://localhost:8100 --arquivo=outro.json
 */
const DEFAULT_FILE = 'seed-tenant.local.json';
const DEFAULT_API = 'http://localhost:8100';

class SeedError extends Error {}

function fail(message) {
  throw new SeedError(message);
}

function arg(name, fallback) {
  const found = process.argv.find((a) => a.startsWith(`--${name}=`));
  return found ? found.slice(name.length + 3) : fallback;
}

async function main() {
  const { readFile } = await import('node:fs/promises');
  const path = arg('arquivo', DEFAULT_FILE);
  const api = arg('api', DEFAULT_API).replace(/\/+$/, '');

  let raw;
  try {
    raw = await readFile(path, 'utf8');
  } catch {
    fail(
      `não encontrei "${path}".\n` +
        `  Copie o seed-tenant.example.json para seed-tenant.local.json e preencha com seus dados.`,
    );
  }

  const seed = JSON.parse(raw);
  if (!seed.tenantId) fail('o arquivo precisa de "tenantId".');

  const ctx = { api, tenantId: seed.tenantId, created: 0, existed: 0 };

  console.log(`Cadastro do tenant ${seed.tenantId}`);
  console.log(`  API: ${api}\n`);

  await seedPayerProfile(ctx, seed.payerProfile);
  await seedPayees(ctx, seed.payees ?? []);
  await seedTrustedOrigins(ctx, seed.trustedOrigins ?? []);

  console.log(`\n${ctx.created} criados, ${ctx.existed} já existiam.`);
}

/**
 * @param options.domainIdempotent
 *   Endpoint cujo agregado já absorve a repetição em silêncio (não devolve 409). Acrescentar um
 *   documento que já está lá é no-op no domínio, então contá-lo como "criado" faria o resumo
 *   mentir na segunda execução. Marcado com `~` e fora das contagens.
 */
async function post(ctx, path, body, label, options = {}) {
  const url = `${ctx.api}/api/v1/${ctx.tenantId}${path}`;

  let response;
  try {
    response = await fetch(url, {
      method: 'POST',
      headers: { 'content-type': 'application/json', 'x-requestid': crypto.randomUUID() },
      body: JSON.stringify(body),
    });
  } catch (cause) {
    fail(`a API não respondeu em ${ctx.api}. Ela está no ar? (${cause.message})`);
  }

  // Já cadastrado é desfecho normal de uma segunda execução, não erro.
  if (response.status === 409) {
    ctx.existed++;
    console.log(`  = ${label}`);
    return null;
  }

  if (!response.ok) {
    const text = await response.text();
    const payload = text ? tryJson(text) : null;
    fail(`${label}\n  HTTP ${response.status} — ${payload?.id ?? ''} ${payload?.message ?? text}`);
  }

  if (options.domainIdempotent) {
    console.log(`  ~ ${label}`);
    return tryJson(await response.text());
  }

  ctx.created++;
  console.log(`  + ${label}`);

  return tryJson(await response.text());
}

function tryJson(text) {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

/**
 * O perfil do pagador é UM por tenant, e é dele que saem as senhas de PDF.
 *
 * Os documentos adicionais não são zelo excessivo: no corpus real, o único PDF cifrado que abriu
 * abriu por um documento ADICIONAL, não pelo principal.
 */
async function seedPayerProfile(ctx, profile) {
  if (!profile) return;

  console.log('PayerProfile');
  await post(
    ctx,
    '/payer-profile',
    { kind: profile.kind, legalName: profile.legalName, primaryTaxId: profile.primaryTaxId },
    `${profile.legalName} (${profile.primaryTaxId})`,
  );

  // O agregado ignora documento repetido em silêncio, então este endpoint nunca devolve 409.
  for (const taxId of profile.additionalTaxIds ?? [])
    await post(
      ctx,
      '/payer-profile/tax-ids',
      { taxId },
      `documento adicional ${taxId}`,
      { domainIdempotent: true },
    );
}

/** Beneficiários conhecidos. Além de alimentar as verificações, salvam o item da quarentena. */
async function seedPayees(ctx, payees) {
  if (payees.length === 0) return;

  console.log('\nPayees');
  for (const payee of payees) {
    const created = await post(
      ctx,
      '/payees',
      {
        legalName: payee.legalName,
        taxId: payee.taxId,
        amountPolicyKind: payee.amountPolicyKind ?? 'Unbounded',
        expectedAmount: payee.expectedAmount ?? null,
        tolerancePercent: payee.tolerancePercent ?? null,
        minAmount: payee.minAmount ?? null,
        maxAmount: payee.maxAmount ?? null,
      },
      `${payee.legalName} (${payee.taxId})`,
    );

    // Sem o id não dá para pendurar apelido — acontece quando o beneficiário já existia.
    if (!created?.id) continue;

    for (const alias of payee.aliases ?? [])
      await post(ctx, `/payees/${created.id}/aliases`, { alias }, `  apelido "${alias}"`);
  }
}

/**
 * Remetentes conhecidos. É o que transforma "não reconheci" em quarentena revisável em vez de
 * descarte — e é justamente por isso que cadastrá-los ANTES de varrer muda o que se consegue medir.
 */
async function seedTrustedOrigins(ctx, origins) {
  if (origins.length === 0) return;

  console.log('\nTrustedOrigins');
  for (const origin of origins) {
    await post(
      ctx,
      '/trusted-origins',
      {
        kind: origin.kind,
        value: origin.value,
        decision: origin.decision ?? 'Trusted',
        decidedBy: origin.decidedBy ?? ctx.tenantId,
        note: origin.note ?? null,
      },
      `${origin.value} (${origin.decision ?? 'Trusted'})`,
    );
  }
}

main().catch((error) => {
  if (error instanceof SeedError) {
    console.error(`\nERRO: ${error.message}`);
  } else {
    console.error('\nFALHA INESPERADA:');
    console.error(error);
  }
  process.exitCode = 1;
});
