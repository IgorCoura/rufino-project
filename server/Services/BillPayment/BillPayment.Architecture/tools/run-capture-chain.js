#!/usr/bin/env node
/**
 * ENSAIO DE PONTA A PONTA — a cadeia de captura contra uma caixa de e-mail REAL.
 *
 * Diferente das sondas irmãs, esta ferramenta NÃO fala com o provedor. Ela dirige a API do
 * BillPayment pela borda HTTP e deixa a aplicação fazer o trabalho — que é justamente o que
 * nenhum teste com transporte falso prova:
 *
 *   1. `ConnectCaptureSource` guarda o segredo no cofre e a prova de acesso passa de verdade?
 *   2. A delta query real devolve mensagens, e cada anexo vira um `CaptureItem`?
 *   3. `ProcessCaptureItem` baixa o anexo, a cascata determinística lê, e a triagem decide?
 *   4. O que a triagem manda guardar chega mesmo ao balde S3 (assinatura, região, permissão)?
 *   5. Qual a distribuição real de desfechos numa caixa de uso misto?
 *
 * A pergunta 5 é a que muda decisão. O corpus de 41 boletos mediu a CASCATA; esta medição é
 * da CAIXA — quantos anexos chegam, que fração é conta a pagar, e quanto o descarte
 * determinístico realmente varre. É o número que dimensiona o custo da 2.4.
 *
 * ---------------------------------------------------------------------------
 * SEGURANÇA — leia antes de rodar
 *
 * - O client secret é lido SÓ de variável de ambiente, viaja SÓ no corpo do POST que o
 *   deposita no cofre, e NUNCA é impresso nem gravado.
 * - Assunto e remetente NÃO são impressos por padrão — são dados de cliente. A saída é
 *   estrutural (contagens por desfecho). `--verboso` mostra domínio do remetente e assunto,
 *   que é o mínimo para diagnosticar um descarte indevido.
 * - Isto ESCREVE: cria uma `CaptureSource`, deposita credencial no cofre, ingere itens e
 *   grava anexos no balde. Não é read-only, e por isso exige --confirmar.
 * - Na caixa não escreve nada: `Mail.Read` não permitiria. Nenhuma mensagem é marcada como
 *   lida, movida ou apagada.
 * - Ao terminar, `--desconectar` remove a fonte e a credencial do cofre.
 *
 * ---------------------------------------------------------------------------
 * PRÉ-REQUISITOS
 *
 *   docker compose up -d billpayment.db billpayment.storage billpayment.storage-init
 *
 *   # PowerShell — a API sobe com estas variáveis; os valores ficam só na sessão
 *   $env:Graph__Enabled                 = "true"
 *   $env:Capture__Enabled               = "true"
 *   $env:Capture__PollingInterval       = "01:00:00"   # varredura só pelo endpoint manual
 *   $env:Storage__ServiceUrl            = "http://localhost:8103"
 *   $env:Storage__AccessKey             = "billpayment"
 *   $env:Storage__SecretKey             = "billpayment-dev"
 *   $env:Storage__AuthenticationRegion  = "us-east-1"  # "garage" quando apontar para o Garage
 *   $env:Secrets__MasterKey             = "<32 bytes em base64>"
 *   dotnet run --project BillPayment.API
 *
 *   # Gerar a master key (funciona no Windows PowerShell 5.1 E no 7+; a forma curta
 *   # [RandomNumberGenerator]::GetBytes(32) é só do 7+ e falha com MethodNotFound no 5.1):
 *   $bytes = New-Object byte[] 32
 *   [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
 *   [Convert]::ToBase64String($bytes)
 *
 * ---------------------------------------------------------------------------
 * USO
 *
 *   $env:GRAPH_DIRECTORY_ID = "<Directory (tenant) ID>"
 *   $env:GRAPH_CLIENT_ID    = "<Application (client) ID>"
 *   $env:GRAPH_CLIENT_SECRET= "<Value do client secret>"
 *   $env:GRAPH_MAILBOX      = "<caixa@empresa.com.br>"
 *
 *   # A API pelo docker-compose responde em 8100; pelo `dotnet run`, em 5269 (o default).
 *   # O token e obrigatorio: a API exige autenticacao, e o tenant do ensaio precisa estar no
 *   # claim `tenants` de quem o emitiu — senao a resposta e 403 em vez de 401.
 *   $env:BILLPAYMENT_TOKEN  = "<access token do Keycloak>"
 *
 *   node run-capture-chain.js --confirmar --api=http://localhost:8100
 *   node run-capture-chain.js --confirmar --verboso
 *   node run-capture-chain.js --confirmar --desconectar
 *
 *   # ao terminar
 *   Remove-Item Env:\GRAPH_CLIENT_SECRET
 */
const CONFIRM_FLAG = '--confirmar';

const ENV = {
  directoryId: 'GRAPH_DIRECTORY_ID',
  clientId: 'GRAPH_CLIENT_ID',
  clientSecret: 'GRAPH_CLIENT_SECRET',
  mailbox: 'GRAPH_MAILBOX',
};

const DEFAULT_API = 'http://localhost:5269';

/** Tenant fixo: reexecutar o ensaio deve reencontrar a mesma fonte, não criar uma nova. */
const DEFAULT_TENANT = '0195a1f0-0000-7000-8000-00000000f001';

/**
 * Quanto esperar o worker de processamento drenar a fila antes de desistir.
 *
 * Medido em 2026-08-11 contra a caixa real: ~0,5s por artefato (até 1,9s), dominado pelo download
 * do anexo no provedor. A primeira varredura de uma caixa antiga traz centenas de itens, então o
 * teto tem que ser minutos, não segundos — com 2 minutos o ensaio desistia no meio e relatava uma
 * fila que ainda estava andando.
 */
const DRAIN_TICKS = 240;
const DRAIN_INTERVAL_MS = 3000;

const verbose = process.argv.includes('--verboso');
const disconnect = process.argv.includes('--desconectar');

/**
 * Erro esperado (configuração errada, API fora do ar, domínio recusando), distinto de defeito.
 *
 * Lança em vez de chamar `process.exit()`: encerrar o processo com uma conexão HTTP ainda
 * aberta faz o libuv abortar no Windows com `UV_HANDLE_CLOSING`, e o stack trace do abort
 * esconde a mensagem que interessa.
 */
class RunError extends Error {}

function fail(message) {
  throw new RunError(message);
}

function readConfig() {
  if (!process.argv.includes(CONFIRM_FLAG)) {
    fail(
      `este ensaio ESCREVE: conecta uma caixa REAL, deposita credencial no cofre e grava\n` +
        `  anexos no balde. Confirme com ${CONFIRM_FLAG}.`,
    );
  }

  // O default é a porta do `dotnet run`; o docker-compose publica em 8100. Errar isso é o
  // tropeço mais fácil do ensaio, então há duas formas de dizer, e a flag vence a variável.
  const flag = process.argv.find((a) => a.startsWith('--api='));

  const config = {
    api: (flag?.slice('--api='.length) || process.env.BILLPAYMENT_API || DEFAULT_API).replace(/\/+$/, ''),
    tenantId: process.env.BILLPAYMENT_TENANT_ID || DEFAULT_TENANT,
    token: (
      process.argv.find((a) => a.startsWith('--token='))?.slice('--token='.length) ||
      process.env.BILLPAYMENT_TOKEN ||
      ''
    ).trim(),
  };

  if (!config.token) {
    fail(
      'informe o access token: --token=<jwt> ou $env:BILLPAYMENT_TOKEN.\n' +
        '  A API exige autenticacao; sem token o ensaio para no primeiro 401.',
    );
  }

  for (const [key, name] of Object.entries(ENV)) {
    const value = process.env[name];
    if (!value || !value.trim()) fail(`variável de ambiente ${name} não definida.`);
    config[key] = value.trim();
  }

  return config;
}

async function call(config, method, path, body) {
  const url = `${config.api}/api/v1/${config.tenantId}${path}`;

  let response;
  try {
    response = await fetch(url, {
      method,
      headers: {
        'content-type': 'application/json',
        // Idempotência: cada chamada do ensaio é uma intenção distinta.
        'x-requestid': crypto.randomUUID(),
        authorization: `Bearer ${config.token}`,
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch (cause) {
    fail(
      `a API não respondeu em ${config.api}. (${cause.message})\n` +
        `  Confira a porta: docker-compose publica em 8100, 'dotnet run' em 5269.\n` +
        `  Aponte com --api=http://localhost:8100 ou com $env:BILLPAYMENT_API.`,
    );
  }

  const text = await response.text();
  const payload = text ? safeJson(text) : null;

  if (!response.ok) {
    const id = payload?.id ? `${payload.id} · ` : '';
    const message = payload?.message || text || '(sem corpo)';
    fail(`HTTP ${response.status} em ${method} ${path}\n  ${id}${message}`);
  }

  return payload;
}

function safeJson(text) {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

/**
 * Reencontra a fonte antes de criar: o endereço é único por tenant, e uma segunda tentativa
 * bateria em BLP.CPS10 em vez de continuar o ensaio.
 */
async function ensureSource(config) {
  const page = await call(config, 'GET', '/capture-sources?limit=100');
  const existing = (page?.items || []).find(
    (s) => s.address?.toLowerCase() === config.mailbox.toLowerCase(),
  );

  if (existing) {
    console.log(`Fonte já conectada · ${existing.id}`);
    console.log(`  cursor guardado: ${existing.hasSyncCursor ? 'sim' : 'não'}`);
    if (existing.lastSyncError) console.log(`  último erro: ${existing.lastSyncError}`);
    return existing.id;
  }

  const credential = JSON.stringify({
    directoryId: config.directoryId,
    clientId: config.clientId,
    clientSecret: config.clientSecret,
  });

  const created = await call(config, 'POST', '/capture-sources', {
    kind: 'MicrosoftGraphMailbox',
    displayName: 'Ensaio de ponta a ponta',
    address: config.mailbox,
    credential,
    folderPath: null,
  });

  console.log(`Fonte conectada · ${created.id}`);
  console.log(
    `  prova de acesso: passou (o segredo foi para o cofre e nunca sai por resposta nenhuma)`,
  );
  if (created.alreadyMonitoredByAnotherAccount) {
    console.log('  aviso ADR-008: esta caixa já é monitorada por outra conta');
  }

  return created.id;
}

async function sync(config, sourceId) {
  const result = await call(config, 'POST', `/capture-sources/${sourceId}/sync`);

  console.log('');
  console.log(`Varredura · ${result.status}`);
  console.log(`  itens ingeridos:            ${result.ingestedItems}`);
  console.log(`  já ingeridos (idempotência): ${result.skippedAsAlreadyIngested}`);

  return result;
}

/**
 * Espera o worker de processamento esvaziar a fila.
 *
 * Não força nada: o `CaptureProcessingBackgroundService` é quem baixa, extrai e tria. Se
 * sobrar item em `Received` depois do teto, isso É o resultado — significa que o worker não
 * está rodando ou que algo estoura em todo ciclo.
 */
async function drain(config) {
  console.log('\nProcessando (a fila anda a ~2 artefatos por segundo)...');

  for (let tick = 0; tick < DRAIN_TICKS; tick++) {
    const items = await listAll(config);
    const pending = items.filter((i) => i.status === 'Received').length;

    if (pending === 0) {
      console.log('  fila vazia.');
      return items;
    }

    // Contagem em vez de ponto: numa fila de centenas, uma fileira de pontos não diz se está
    // andando ou travada — e essa é exatamente a pergunta de quem está olhando.
    if (tick % 5 === 0) console.log(`  ${pending} pendentes`);

    await new Promise((resolve) => setTimeout(resolve, DRAIN_INTERVAL_MS));
  }

  console.log('  teto atingido.');
  console.log(
    '  AVISO: ainda há item em Received. O worker de captura está ligado (Capture:Enabled)?',
  );
  return listAll(config);
}

async function listAll(config) {
  const items = [];
  let cursor = null;

  do {
    const query = cursor ? `?limit=100&cursor=${encodeURIComponent(cursor)}` : '?limit=100';
    const page = await call(config, 'GET', `/capture-items${query}`);
    items.push(...(page?.items || []));
    cursor = page?.nextCursor || null;
  } while (cursor);

  return items;
}

function report(items, sync) {
  const byStatus = new Map();
  for (const item of items) byStatus.set(item.status, (byStatus.get(item.status) || 0) + 1);

  console.log('');
  console.log('Desfechos');
  console.log('  ─────────────────────────────────────────────');

  // Descartado não aparece na lista: o item deixa de existir. A contagem sai da diferença
  // entre o que a varredura ingeriu e o que sobrou — que é exatamente o que se quer medir.
  const survived = items.length;
  const dropped = Math.max(0, sync.ingestedItems - survived);

  for (const [status, count] of [...byStatus].sort((a, b) => b[1] - a[1])) {
    console.log(`  ${status.padEnd(14)} ${String(count).padStart(4)}`);
  }
  if (dropped > 0) console.log(`  ${'Descartado'.padEnd(14)} ${String(dropped).padStart(4)}  (não é boleto — nem item, nem arquivo)`);

  console.log('  ─────────────────────────────────────────────');
  console.log(`  ${'ingeridos'.padEnd(14)} ${String(sync.ingestedItems).padStart(4)}`);

  if (sync.ingestedItems > 0) {
    const kept = ((survived / sync.ingestedItems) * 100).toFixed(1);
    console.log(`\n  ${kept}% dos anexos varridos sobreviveram à triagem determinística.`);
  }

  const parsed = items.filter((i) => i.status === 'Parsed');
  if (parsed.length > 0) {
    const methods = new Map();
    for (const item of parsed) {
      const key = item.extractionMethod || '(nulo)';
      methods.set(key, (methods.get(key) || 0) + 1);
    }
    console.log('\n  Degrau que resolveu:');
    for (const [method, count] of methods) console.log(`    ${method.padEnd(14)} ${count}`);

    const unlocked = parsed.filter((i) => i.unlockedBy);
    if (unlocked.length > 0) {
      console.log(`\n  ${unlocked.length} abriram com senha derivada:`);
      for (const item of unlocked) console.log(`    ${item.unlockedBy}`);
    }
  }

  if (!verbose) return;

  console.log('\nItens (--verboso)');
  for (const item of items) {
    const domain = item.sender?.split('@')[1] || '(sem remetente)';
    console.log(`  ${item.status.padEnd(14)} ${domain.padEnd(30)} ${item.subject || ''}`);
    if (item.reason) console.log(`  ${''.padEnd(14)} motivo: ${item.reason}`);
  }
}

async function main() {
  const config = readConfig();

  console.log('Ensaio de ponta a ponta — cadeia de captura');
  console.log(`  API:    ${config.api}`);
  console.log(`  tenant: ${config.tenantId}`);
  console.log(`  caixa:  ${config.mailbox}`);
  console.log('');

  const sourceId = await ensureSource(config);
  const result = await sync(config, sourceId);
  const items = await drain(config);

  report(items, result);

  if (disconnect) {
    await call(config, 'DELETE', `/capture-sources/${sourceId}`);
    console.log('\nFonte desconectada e credencial removida do cofre.');
    console.log('Os itens já ingeridos permanecem, por desenho.');
  }
}

main().catch((error) => {
  if (error instanceof RunError) {
    console.error(`\nERRO: ${error.message}`);
  } else {
    console.error('\nFALHA INESPERADA:');
    console.error(error);
  }
  process.exitCode = 1;
});
