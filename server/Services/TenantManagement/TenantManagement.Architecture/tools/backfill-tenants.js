#!/usr/bin/env node
/**
 * Backfill de tenants a partir dos cadastros que já existem nos produtos.
 *
 * Por que existe: o `TenantId` já circula em produção — é o `companies` do token no
 * PeopleManagement e o `{tenantId}` da rota no BillPayment. Cadastrar esses tenants com um Id
 * NOVO obrigaria a reemitir todo o acesso e a reapontar todo cadastro local. Este script
 * preserva o Guid, e é isso que faz a migração ser invisível para quem usa.
 *
 * O cadastro entra PELA API, nunca por SQL: assim cada documento passa por `TaxId.Parse`, pelo
 * dígito verificador e por toda invariante do agregado. Documento inválido falha aqui, no
 * backfill, e não meses depois numa consulta oficial.
 *
 * Idempotente: 409 conta como "já existia".
 *
 * Uso:
 *   node backfill-tenants.js --api=http://localhost:8110 --file=./tenants.local.json [--dry-run]
 *
 * O arquivo de entrada NÃO é versionado (contém CNPJ, CPF e e-mail reais). O versionado é o
 * `backfill-tenants.example.json`. Para produzi-lo a partir do PeopleManagement:
 *
 *   psql -d PeopleManagementDb -At -c "\
 *     SELECT json_agg(row_to_json(t)) FROM ( \
 *       SELECT id, 'Company' AS kind, corporate_name AS legal_name, fantasy_name AS trade_name, \
 *              cnpj AS primary_tax_id, email AS contact_email, phone AS contact_phone, \
 *              json_build_object('zipCode', zip_code, 'street', street, 'number', number, \
 *                'complement', complement, 'neighborhood', neighborhood, 'city', city, \
 *                'state', state, 'country', country) AS address \
 *       FROM people_management.companies) t" > tenants.local.json
 *
 * Cada item precisa de `ownerEmail` — quem responde pelo tenant. Sem ele o tenant nasceria sem
 * dono, que é o estado que ninguém percebe até precisar dele.
 */

const fs = require('node:fs');
const crypto = require('node:crypto');

const args = Object.fromEntries(
  process.argv.slice(2).map((a) => {
    const [k, v] = a.replace(/^--/, '').split('=');
    return [k, v ?? true];
  }),
);

const api = (args.api || 'http://localhost:8110').replace(/\/$/, '');
const file = args.file;
const dryRun = Boolean(args['dry-run']);
const token = args.token || process.env.TENANT_MANAGEMENT_TOKEN || '';

if (!file) {
  console.error('Informe o arquivo de entrada: --file=./tenants.local.json');
  process.exit(1);
}

const items = JSON.parse(fs.readFileSync(file, 'utf8'));
if (!Array.isArray(items)) {
  console.error('O arquivo precisa conter um array de tenants.');
  process.exit(1);
}

const summary = { created: 0, existed: 0, failed: 0 };

async function post(item) {
  const body = {
    id: item.id,
    kind: item.kind || 'Company',
    legalName: item.legalName || item.legal_name,
    tradeName: item.tradeName ?? item.trade_name ?? null,
    primaryTaxId: item.primaryTaxId || item.primary_tax_id,
    contactEmail: item.contactEmail || item.contact_email,
    contactPhone: item.contactPhone ?? item.contact_phone ?? null,
    address: item.address,
    ownerEmail: item.ownerEmail || item.owner_email,
    products: item.products ?? null,
  };

  if (dryRun) {
    console.log(`[dry-run] ${body.id} ${body.legalName} (${body.primaryTaxId})`);
    return;
  }

  const response = await fetch(`${api}/api/v1/tenants`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      // Determinístico por tenant: reexecutar o backfill não duplica nem gera marca nova.
      'x-requestid': crypto
        .createHash('sha1')
        .update(`backfill:${body.id}`)
        .digest('hex')
        .slice(0, 32)
        .replace(/^(.{8})(.{4})(.{4})(.{4})(.{12})$/, '$1-$2-$3-$4-$5'),
      ...(token ? { authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(body),
  });

  if (response.ok) {
    summary.created += 1;
    console.log(`ok      ${body.id} ${body.legalName}`);
    return;
  }

  const text = await response.text();

  if (response.status === 409) {
    summary.existed += 1;
    console.log(`existe  ${body.id} ${body.legalName}`);
    return;
  }

  summary.failed += 1;
  console.error(`FALHOU  ${body.id} ${body.legalName} -> ${response.status} ${text}`);
}

(async () => {
  for (const item of items) {
    await post(item);
  }

  console.log(
    `\ncriados: ${summary.created} | já existiam: ${summary.existed} | falharam: ${summary.failed}`,
  );

  // Falha visível: um backfill parcial que sai com 0 parece um backfill completo no CI.
  process.exit(summary.failed > 0 ? 1 : 0);
})();
