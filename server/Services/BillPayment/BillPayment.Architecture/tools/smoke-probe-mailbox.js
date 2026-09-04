#!/usr/bin/env node
/**
 * SONDA DE FUMAÇA — leitura de caixa no Microsoft Graph.
 *
 * Responde as perguntas que nenhum teste com transporte falso consegue responder:
 *
 *   1. O trio (directoryId, clientId, clientSecret) obtém token para o escopo do Graph?
 *   2. A Application Access Policy deixa esse aplicativo alcançar ESTA caixa — e só ela?
 *   3. A delta query devolve `@odata.deltaLink`, e os campos do `$select` vêm preenchidos?
 *   4. Que fração das mensagens tem anexo, e com que content-type e tamanho reais?
 *   5. O `id` de anexo é estável o bastante para servir de `ArtifactKey`?
 *
 * A pergunta 4 é a que muda decisão: os filtros de anexo do `GraphOptions` (allowlist de
 * content-type e teto de tamanho) foram escolhidos sem dados. Se a caixa real usar
 * `application/octet-stream` para PDF — o que alguns emissores fazem —, ou se boleto vier
 * como `.zip`, a allowlist descarta boleto em silêncio, que é o pior desfecho possível.
 *
 * ---------------------------------------------------------------------------
 * SEGURANÇA — leia antes de rodar
 *
 * - O client secret é lido SÓ de variável de ambiente e NUNCA é impresso nem gravado.
 * - O token obtido NUNCA é impresso.
 * - Esta sonda é ESTRITAMENTE read-only: só faz GET, e só nos três caminhos listados.
 *   Não marca como lida, não move, não apaga. A permissão `Mail.Read` nem permitiria.
 * - Assunto e remetente NÃO são impressos por padrão — são dados de cliente. A saída é
 *   estrutural (contagens, tipos, tamanhos). Use --verboso para ver domínio do remetente e
 *   nome de arquivo, que é o mínimo para diagnosticar um filtro descartando boleto.
 * - Revogue o client secret assim que terminar, se ele foi criado só para a sonda.
 *
 * ---------------------------------------------------------------------------
 * USO
 *
 *   # PowerShell — os valores ficam só na sessão, não no disco
 *   $env:GRAPH_DIRECTORY_ID = "<Directory (tenant) ID>"
 *   $env:GRAPH_CLIENT_ID    = "<Application (client) ID>"
 *   $env:GRAPH_CLIENT_SECRET= "<Value do client secret>"
 *   $env:GRAPH_MAILBOX      = "<contas@empresa.com.br>"
 *
 *   node smoke-probe-mailbox.js --confirmar
 *   node smoke-probe-mailbox.js --confirmar --verboso
 *
 *   # ao terminar
 *   Remove-Item Env:\GRAPH_CLIENT_SECRET
 */
const CONFIRM_FLAG = '--confirmar';
const GRAPH = 'https://graph.microsoft.com/v1.0';
const LOGIN = 'https://login.microsoftonline.com';

const ENV = {
  directoryId: 'GRAPH_DIRECTORY_ID',
  clientId: 'GRAPH_CLIENT_ID',
  clientSecret: 'GRAPH_CLIENT_SECRET',
  mailbox: 'GRAPH_MAILBOX',
};

const verbose = process.argv.includes('--verboso');

/**
 * Erro esperado da sonda (configuração errada, provedor recusando), distinto de defeito.
 *
 * Lança em vez de chamar `process.exit()`: encerrar o processo com uma conexão HTTP ainda
 * aberta faz o libuv abortar no Windows com `UV_HANDLE_CLOSING`, e o stack trace do abort
 * esconde a mensagem que interessa.
 */
class ProbeError extends Error {}

function fail(message) {
  throw new ProbeError(message);
}

function readConfig() {
  if (!process.argv.includes(CONFIRM_FLAG)) {
    fail(
      `esta sonda lê uma caixa de e-mail REAL.\n` +
        `  Confirme com ${CONFIRM_FLAG}. Ela é read-only: só faz GET.`,
    );
  }

  const config = {};
  for (const [key, name] of Object.entries(ENV)) {
    const value = process.env[name];
    if (!value || !value.trim()) fail(`variável de ambiente ${name} não definida.`);
    config[key] = value.trim();
  }
  return config;
}

async function getToken({ directoryId, clientId, clientSecret }) {
  const body = new URLSearchParams({
    client_id: clientId,
    client_secret: clientSecret,
    scope: 'https://graph.microsoft.com/.default',
    grant_type: 'client_credentials',
  });

  const response = await fetch(`${LOGIN}/${directoryId}/oauth2/v2.0/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body,
  });

  const text = await response.text();

  if (!response.ok) {
    // O corpo do Entra ID traz error/error_description e NÃO contém o segredo.
    let hint = '';
    try {
      const parsed = JSON.parse(text);
      hint = `${parsed.error ?? '?'} — ${(parsed.error_description ?? '').split('\r')[0]}`;
    } catch {
      hint = `HTTP ${response.status}`;
    }
    fail(`o Entra ID recusou o token: ${hint}`);
  }

  const parsed = JSON.parse(text);
  if (!parsed.access_token) fail('o Entra ID respondeu 200 sem access_token.');

  return { token: parsed.access_token, expiresIn: parsed.expires_in };
}

async function graphGet(url, token) {
  const response = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
  const text = await response.text();
  let body = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = null;
  }
  return { status: response.status, ok: response.ok, body };
}

function describeFailure(status, body) {
  const code = body?.error?.code ?? '(sem code)';
  const message = (body?.error?.message ?? '').split('\n')[0];

  const traducao = {
    401: 'Denied — token inválido ou sem o escopo esperado.',
    403: 'Denied — Application Access Policy barrando o app nesta caixa, ou Mail.Read sem consentimento de admin.',
    404: 'Denied — caixa não encontrada. Confira o UPN (não o alias).',
    410: 'CursorExpired — deltaLink velho demais (não deveria acontecer na primeira varredura).',
    429: 'Unavailable — throttling.',
  }[status];

  return `HTTP ${status} · ${code}${message ? ` · ${message}` : ''}\n      → ${traducao ?? 'Unavailable — falha do provedor.'}`;
}

function domainOf(address) {
  const at = String(address ?? '').lastIndexOf('@');
  return at < 0 ? '(sem domínio)' : address.slice(at + 1).toLowerCase();
}

async function main() {
  const config = readConfig();
  const mailbox = encodeURIComponent(config.mailbox);

  console.log('\n  SONDA DE CAIXA — Microsoft Graph (read-only)');
  console.log(`  Caixa: ${config.mailbox}`);
  console.log('  ' + '-'.repeat(70));

  // ---- 1. Token -----------------------------------------------------------
  const { token, expiresIn } = await getToken(config);
  console.log(`\n  [1] Token de aplicativo ....... OK (expira em ${expiresIn}s)`);

  // ---- 2. Prova de acesso (o mesmo caminho do ProbeAccessAsync) ------------
  const probe = await graphGet(
    `${GRAPH}/users/${mailbox}/mailFolders/inbox/messages?$top=1&$select=id`,
    token,
  );

  if (!probe.ok) {
    console.log(`\n  [2] Prova de acesso ........... FALHOU`);
    console.log(`      ${describeFailure(probe.status, probe.body)}`);
    console.log(
      '\n  Este é exatamente o desfecho que faria POST /capture-sources recusar a conexão.\n',
    );
    process.exitCode = 2;
    return;
  }
  console.log('  [2] Prova de acesso ........... OK');

  // ---- 3. Delta query -----------------------------------------------------
  const deltaUrl =
    `${GRAPH}/users/${mailbox}/mailFolders/inbox/messages/delta` +
    `?$select=id,subject,receivedDateTime,hasAttachments,from&$top=50`;

  const delta = await graphGet(deltaUrl, token);

  if (!delta.ok) {
    console.log(`\n  [3] Delta query ............... FALHOU`);
    console.log(`      ${describeFailure(delta.status, delta.body)}`);
    process.exitCode = 2;
    return;
  }

  const messages = delta.body?.value ?? [];
  const temDelta = Boolean(delta.body['@odata.deltaLink']);
  const temNext = Boolean(delta.body['@odata.nextLink']);
  const comAnexo = messages.filter((m) => m.hasAttachments === true);
  const removidas = messages.filter((m) => m['@removed']).length;

  console.log('  [3] Delta query ............... OK');
  console.log(`      mensagens na 1ª página ...... ${messages.length}`);
  console.log(`      com anexo ................... ${comAnexo.length}`);
  console.log(`      marcadas como removidas ..... ${removidas}`);
  console.log(`      @odata.deltaLink ............ ${temDelta ? 'presente (varredura terminou)' : 'ausente'}`);
  console.log(`      @odata.nextLink ............. ${temNext ? 'presente (há mais páginas)' : 'ausente'}`);

  const faltando = ['id', 'receivedDateTime', 'hasAttachments', 'from'].filter(
    (campo) => messages.some((m) => !m['@removed'] && m[campo] === undefined),
  );
  console.log(
    `      campos do $select ........... ${faltando.length === 0 ? 'todos presentes' : 'AUSENTES: ' + faltando.join(', ')}`,
  );

  if (!temDelta && !temNext) {
    console.log('\n      ATENÇÃO: sem deltaLink nem nextLink — o cursor não avançaria.');
  }

  // ---- 4. Anexos: a medição que muda os filtros ---------------------------
  console.log('\n  [4] Anexos das mensagens com hasAttachments');

  const tipos = new Map();
  let totalAnexos = 0;
  let inline = 0;
  let acimaDoTeto = 0;
  let semId = 0;
  const TETO = 20 * 1024 * 1024;
  const ALLOWLIST = ['application/pdf', 'application/octet-stream', 'image/png', 'image/jpeg'];
  const foraDaAllowlist = new Map();

  for (const message of comAnexo.slice(0, 25)) {
    const anexos = await graphGet(
      `${GRAPH}/users/${mailbox}/messages/${encodeURIComponent(message.id)}` +
        `/attachments?$select=id,name,contentType,size,isInline`,
      token,
    );

    if (!anexos.ok) {
      console.log(`      (falha ao listar anexos de uma mensagem: HTTP ${anexos.status})`);
      continue;
    }

    for (const anexo of anexos.body?.value ?? []) {
      totalAnexos++;
      if (!anexo.id) semId++;
      if (anexo.isInline === true) inline++;
      if ((anexo.size ?? 0) > TETO) acimaDoTeto++;

      const tipo = (anexo.contentType ?? '(sem contentType)').split(';')[0].trim();
      tipos.set(tipo, (tipos.get(tipo) ?? 0) + 1);

      const ehCandidato = anexo.isInline !== true && (anexo.size ?? 0) <= TETO;
      if (ehCandidato && !ALLOWLIST.includes(tipo.toLowerCase())) {
        const nome = anexo.name ?? '(sem nome)';
        foraDaAllowlist.set(tipo, (foraDaAllowlist.get(tipo) ?? []).concat(verbose ? nome : []));
      }

      if (verbose) {
        console.log(
          `        · ${String(anexo.name ?? '(sem nome)').padEnd(40)} ${tipo.padEnd(30)} ` +
            `${String(anexo.size ?? 0).padStart(9)} B${anexo.isInline ? '  [inline]' : ''}`,
        );
      }
    }
  }

  console.log(`      anexos examinados ........... ${totalAnexos}`);
  console.log(`      inline (descartados) ........ ${inline}`);
  console.log(`      acima do teto de 20 MB ...... ${acimaDoTeto}`);
  console.log(`      sem id (inutilizáveis) ...... ${semId}`);

  if (tipos.size > 0) {
    console.log('\n      content-type observado:');
    for (const [tipo, quantidade] of [...tipos].sort((a, b) => b[1] - a[1])) {
      const dentro = ALLOWLIST.includes(tipo.toLowerCase()) ? ' ✓ na allowlist' : ' ✗ FORA da allowlist';
      console.log(`        ${String(quantidade).padStart(4)}x  ${tipo.padEnd(35)}${dentro}`);
    }
  }

  if (foraDaAllowlist.size > 0) {
    console.log('\n      DECISÃO NECESSÁRIA — tipos fora da allowlist que NÃO são inline nem grandes:');
    for (const [tipo, nomes] of foraDaAllowlist) {
      console.log(`        ${tipo}${nomes.length ? ` (ex.: ${nomes.slice(0, 3).join(', ')})` : ''}`);
    }
    console.log(
      '        Se algum destes carrega boleto, acrescente em GraphOptions.AllowedContentTypes —\n' +
        '        hoje eles seriam descartados em silêncio.',
    );
  }

  if (verbose && comAnexo.length > 0) {
    const dominios = new Map();
    for (const m of comAnexo) {
      const d = domainOf(m.from?.emailAddress?.address);
      dominios.set(d, (dominios.get(d) ?? 0) + 1);
    }
    console.log('\n      remetentes com anexo (por domínio):');
    for (const [dominio, quantidade] of [...dominios].sort((a, b) => b[1] - a[1])) {
      console.log(`        ${String(quantidade).padStart(4)}x  ${dominio}`);
    }
  }

  console.log('\n  ' + '-'.repeat(70));
  console.log('  Sonda concluída. Nenhuma mensagem foi alterada.\n');
}

main().catch((error) => {
  console.error(`\n  ERRO: ${error?.message ?? String(error)}\n`);

  // exitCode em vez de exit(): deixa o Node fechar os sockets antes de terminar.
  process.exitCode = error instanceof ProbeError ? 1 : 3;
});
