namespace BillPayment.Infra.Mailboxes.Graph;

using System.Text;
using BillPayment.Domain.Mailboxes;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.Services;
using BillPayment.Infra.Extraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Adapter de leitura de caixa sobre o Microsoft Graph, por delta query.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Só lê.</strong> Não marca como lida, não move e não apaga — o sistema não altera a
/// caixa de ninguém, e a permissão concedida (<c>Mail.Read</c>) nem permitiria.
/// </para>
/// <para>
/// <strong>Nenhuma exceção de integração escapa daqui</strong>: tudo vira
/// <c>MailboxAccessProbe</c> ou <c>MailboxReadResult</c> com o status certo. Uma caixa fora do ar
/// não pode derrubar a varredura das outras.
/// </para>
/// </remarks>
internal sealed class GraphMailboxReader(
    IHttpClientFactory httpClientFactory,
    ISecretVault vault,
    GraphTokenProvider tokenProvider,
    IDocumentLinkResolver linkResolver,
    IOptions<GraphOptions> options,
    TimeProvider clock,
    ILogger<GraphMailboxReader> logger) : IMailboxReader
{
    /// <summary>
    /// A chave do artefato que representa o corpo da mensagem.
    /// </summary>
    /// <remarks>
    /// Literal fixo porque a chave só precisa distinguir os irmãos de <em>uma</em> mensagem, e o
    /// corpo é um só. Não colide com anexo: no Graph a chave de anexo é um identificador opaco
    /// longo, em base64.
    /// </remarks>
    internal const string BODY_ARTIFACT_KEY = "message-body";

    private readonly GraphOptions _options = options.Value;

    public async Task<MailboxAccessProbe> ProbeAccessAsync(
        string mailboxAddress,
        CredentialRef credential,
        string? folderPath,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var (token, failure) = await AuthenticateAsync(credential, cancellationToken);
        if (token is null)
            return Probe(failure!, now);

        var http = httpClientFactory.CreateClient(GraphHttp.CLIENT_NAME);

        // Resolver a pasta faz parte da prova: apontar para pasta inexistente devolveria zero
        // mensagens sem erro nenhum, e o usuário concluiria que não chega boleto.
        var (folder, folderFailure) = await ResolveFolderAsync(
            http, token, mailboxAddress, folderPath, cancellationToken);

        if (folder is null)
            return Probe(folderFailure!, now);

        // Uma mensagem, um campo. O objetivo é provar alcance, não trazer conteúdo.
        var url = $"{Base}users/{Escape(mailboxAddress)}/mailFolders/{folder}/messages?$top=1&$select=id";
        var (_, readFailure) = await http.GetAsync<GraphMessagePage>(url, token, logger, cancellationToken);

        return readFailure is null ? MailboxAccessProbe.Granted(now) : Probe(readFailure, now);
    }

    /// <summary>
    /// Traduz o caminho da pasta no identificador que o Graph aceita.
    /// </summary>
    /// <remarks>
    /// <c>null</c> vira <c>inbox</c>, que é nome bem-conhecido e dispensa consulta. Caminho com
    /// <c>/</c> é percorrido nível a nível — uma chamada por nível, só na varredura, e a
    /// alternativa (guardar o id da pasta) quebraria silenciosamente se o usuário renomeasse a
    /// pasta no cliente de e-mail.
    /// </remarks>
    private async Task<(string? Folder, GraphFailure? Failure)> ResolveFolderAsync(
        HttpClient http,
        string token,
        string mailboxAddress,
        string? folderPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return ("inbox", null);

        var basePath = $"{Base}users/{Escape(mailboxAddress)}/mailFolders";
        string? currentId = null;

        foreach (var segment in folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Aspas simples em OData escapam duplicando — pasta chamada "Conta's" não pode
            // quebrar o filtro nem virar injeção de consulta.
            var filter = Uri.EscapeDataString($"displayName eq '{segment.Replace("'", "''", StringComparison.Ordinal)}'");
            var url = currentId is null
                ? $"{basePath}?$filter={filter}&$select=id"
                : $"{basePath}/{currentId}/childFolders?$filter={filter}&$select=id";

            var (page, failure) = await http.GetAsync<GraphFolderPage>(url, token, logger, cancellationToken);

            if (failure is not null)
                return (null, failure);

            var match = page!.Value?.FirstOrDefault(f => !string.IsNullOrEmpty(f.Id));

            if (match is null)
            {
                logger.LogWarning("Pasta monitorada não encontrada na caixa.");
                return (null, new GraphFailure(MailboxStatus.Denied, "folder_not_found", null));
            }

            currentId = match.Id;
        }

        return (currentId, null);
    }

    public async Task<MailboxReadResult> ReadAsync(
        string mailboxAddress,
        CredentialRef credential,
        string? folderPath,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var (token, failure) = await AuthenticateAsync(credential, cancellationToken);
        if (token is null)
            return Read(failure!, now);

        var http = httpClientFactory.CreateClient(GraphHttp.CLIENT_NAME);

        // Cursor ilegível é tratado como cursor expirado, e não como defeito: a recuperação é a
        // mesma — descartar e varrer a caixa inteira. Deixar a URL inválida chegar ao HttpClient
        // lançaria InvalidOperationException, que não é falha de transporte e escaparia do
        // adapter, derrubando a varredura em vez de registrá-la.
        if (cursor is not null && !IsAbsoluteHttpUrl(cursor))
        {
            logger.LogWarning("Cursor de sincronização ilegível; a próxima varredura será completa.");
            return MailboxReadResult.CursorExpired("cursor_malformed", null, now);
        }

        string url;
        if (cursor is null)
        {
            var (folder, folderFailure) = await ResolveFolderAsync(
                http, token, mailboxAddress, folderPath, cancellationToken);

            if (folder is null)
                return Read(folderFailure!, now);

            url = InitialDeltaUrl(mailboxAddress, folder);
        }
        else
        {
            // O cursor já carrega a pasta: ele veio de uma varredura dela. Reresolver aqui só
            // gastaria chamada, e mudaria de pasta sem passar por ChangeFolder — que é quem
            // garante o descarte do cursor.
            url = cursor;
        }

        var collected = new List<GraphMessage>();
        string? nextCursor = null;

        for (var page = 0; page < _options.MaxPagesPerSync; page++)
        {
            var (body, pageFailure) = await http.GetAsync<GraphMessagePage>(
                url, token, logger, cancellationToken, _options.PageSize);

            if (pageFailure is not null)
                return Read(pageFailure, now);

            collected.AddRange((body!.Value ?? []).Where(IsIngestable));

            // O deltaLink só aparece na última página; o nextLink continua a MESMA varredura.
            // Guardar o nextLink como cursor é o que permite parar no teto de páginas sem perder
            // o lugar — a varredura seguinte retoma de onde esta parou, em vez de recomeçar.
            nextCursor = body.DeltaLink ?? body.NextLink;

            if (body.DeltaLink is not null || body.NextLink is null)
                break;

            url = body.NextLink;
        }

        var messages = await BuildMessagesAsync(http, token, mailboxAddress, collected, cancellationToken);

        return MailboxReadResult.Ok(messages, nextCursor, now);
    }

    public async Task<ReadOnlyMemory<byte>?> DownloadArtifactAsync(
        string mailboxAddress,
        CredentialRef credential,
        string externalMessageId,
        string artifactKey,
        CancellationToken cancellationToken)
    {
        var (token, _) = await AuthenticateAsync(credential, cancellationToken);
        if (token is null)
            return null;

        var http = httpClientFactory.CreateClient(GraphHttp.CLIENT_NAME);

        if (string.Equals(artifactKey, BODY_ARTIFACT_KEY, StringComparison.Ordinal))
            return await DownloadBodyAsync(http, token, mailboxAddress, externalMessageId, cancellationToken);

        // `/$value` devolve o conteúdo bruto do anexo, sem o envelope JSON do Graph.
        var url = $"{Base}users/{Escape(mailboxAddress)}/messages/{Escape(externalMessageId)}"
            + $"/attachments/{Escape(artifactKey)}/$value";

        return await http.GetBytesAsync(url, token, _options.MaxAttachmentBytes, logger, cancellationToken);
    }

    /// <summary>
    /// Rebusca o corpo da mensagem no momento do processamento, em vez de guardá-lo da varredura.
    /// </summary>
    /// <remarks>
    /// <strong>O corpo carrega a linha digitável e o BR Code</strong> — é instrumento de pagamento,
    /// e quem o tem, paga. Segurá-lo em memória entre a varredura e o processamento o espalharia
    /// por dumps e por qualquer diagnóstico do worker; a segunda leitura custa uma chamada e mantém
    /// o dado sensível com tempo de vida curto, do mesmo jeito que o anexo nunca viaja na listagem.
    /// </remarks>
    private async Task<ReadOnlyMemory<byte>?> DownloadBodyAsync(
        HttpClient http,
        string token,
        string mailboxAddress,
        string externalMessageId,
        CancellationToken cancellationToken)
    {
        var url = $"{Base}users/{Escape(mailboxAddress)}/messages/{Escape(externalMessageId)}?$select=body";

        var (message, failure) = await http.GetAsync<GraphMessage>(url, token, logger, cancellationToken);

        if (failure is not null || string.IsNullOrEmpty(message?.Body?.Content))
            return null;

        return Encoding.UTF8.GetBytes(message.Body.Content);
    }

    /// <summary>Resolve o ponteiro do cofre e troca o segredo por um token de aplicativo.</summary>
    private async Task<(string? Token, GraphFailure? Failure)> AuthenticateAsync(
        CredentialRef credentialRef,
        CancellationToken cancellationToken)
    {
        string raw;
        try
        {
            raw = await vault.ResolveAsync(credentialRef, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Referência inexistente ou que não decifra. A mensagem NÃO entra no motivo: ela
            // pode carregar detalhe do cofre, e o motivo vai para o banco e para a tela.
            logger.LogError(ex, "Não foi possível resolver a credencial da caixa no cofre");
            return (null, new GraphFailure(MailboxStatus.Denied, "credential_unresolvable", null));
        }

        if (!GraphMailboxCredential.TryParse(raw, out var credential))
            return (null, new GraphFailure(MailboxStatus.Denied, "credential_malformed", null));

        return await tokenProvider.AcquireAsync(credential!, cancellationToken);
    }

    /// <summary>
    /// Para cada mensagem com anexo, busca a listagem e converte no que o domínio entende.
    /// </summary>
    /// <remarks>
    /// <para>
    /// É uma chamada por mensagem: a delta query não aceita <c>$expand=attachments</c>. Aceito
    /// porque caixa de contas a pagar tem volume baixo e <c>hasAttachments</c> já descarta a
    /// maioria antes de gastar a chamada.
    /// </para>
    /// <para>
    /// <strong>Desde a 2.5 o corpo também é artefato.</strong> Antes, mensagem sem anexo não virava
    /// item — e a medição de um ano da caixa real mostrou contas que <em>nunca</em> terão anexo: a
    /// Perfil Líder informa por escrito que não envia boleto por e-mail, e a SABESP manda o BR Code
    /// no próprio texto. Quem decide se o corpo vale um item é o <c>BodyCaptureGateService</c>;
    /// sem portão, toda conversa da caixa viraria item e a fila de quarentena ficaria inútil.
    /// </para>
    /// </remarks>
    private async Task<List<MailboxMessage>> BuildMessagesAsync(
        HttpClient http,
        string token,
        string mailboxAddress,
        List<GraphMessage> source,
        CancellationToken cancellationToken)
    {
        var messages = new List<MailboxMessage>();
        var resolvableHosts = linkResolver.ResolvableHosts;

        foreach (var message in source)
        {
            var artifacts = message.HasAttachments == true
                ? await ListAttachmentsAsync(http, token, mailboxAddress, message.Id!, cancellationToken)
                : [];

            if (CarriesPayableBody(message, resolvableHosts))
                artifacts.Add(BodyArtifact(message));

            if (artifacts.Count == 0)
                continue;

            messages.Add(MailboxMessage.From(
                message.Id!,
                message.From?.EmailAddress?.Address ?? string.Empty,
                message.Subject,
                message.ReceivedDateTime ?? clock.GetUtcNow(),
                artifacts));
        }

        return messages;
    }

    private async Task<List<MailboxArtifact>> ListAttachmentsAsync(
        HttpClient http,
        string token,
        string mailboxAddress,
        string messageId,
        CancellationToken cancellationToken)
    {
        var url = $"{Base}users/{Escape(mailboxAddress)}/messages/{Escape(messageId)}"
            + "/attachments?$select=id,name,contentType,size,isInline";

        var (body, failure) = await http.GetAsync<GraphAttachmentPage>(url, token, logger, cancellationToken);

        // Falha ao listar anexo de UMA mensagem não invalida a varredura: a mensagem fica de
        // fora, o cursor não avança além do que foi lido, e ela volta na próxima passagem.
        if (failure is not null)
            return [];

        return (body!.Value ?? [])
            .Where(IsCandidateAttachment)
            .Select(a => MailboxArtifact.From(a.Id!, a.Name, a.ContentType, a.Size ?? 0))
            .ToList();
    }

    /// <summary>
    /// Se o corpo desta mensagem carrega sinal de algo pagável.
    /// </summary>
    /// <remarks>
    /// A conversão para texto e a colheita de links acontecem aqui, na varredura, porque o corpo já
    /// veio na página da delta query — decidir depois obrigaria a baixar de novo, uma chamada por
    /// mensagem, para descartar a maioria.
    /// </remarks>
    private static bool CarriesPayableBody(GraphMessage message, IReadOnlyCollection<string> resolvableHosts)
    {
        var content = message.Body?.Content;

        if (string.IsNullOrWhiteSpace(content))
            return false;

        var isHtml = string.Equals(message.Body!.ContentType, "html", StringComparison.OrdinalIgnoreCase);
        var text = isHtml ? HtmlText.ToPlainText(content) : content;
        var links = isHtml ? HtmlLinkHarvester.Harvest(content) : [];

        return BodyCaptureGateService.ShouldCapture(text, links, resolvableHosts);
    }

    private static MailboxArtifact BodyArtifact(GraphMessage message)
        => MailboxArtifact.From(
            BODY_ARTIFACT_KEY,
            fileName: null,
            string.Equals(message.Body?.ContentType, "html", StringComparison.OrdinalIgnoreCase)
                ? "text/html"
                : "text/plain",
            message.Body?.Content?.Length ?? 0);

    /// <summary>Mensagem sem id, ou removida da pasta desde o último cursor, é ignorada.</summary>
    /// <remarks>
    /// <para>
    /// O que já foi ingerido é trilha de auditoria e não se desfaz porque alguém arrumou a caixa
    /// de entrada — um boleto já pago não pode sumir do histórico por arquivamento do e-mail.
    /// </para>
    /// <para>
    /// <strong>Não exige mais <c>hasAttachments</c>.</strong> Exigir era o que tornava invisível a
    /// conta que só chega por link — e é a maior parte das contas de concessionária hoje. Quem
    /// filtra agora é o portão do corpo, que examina o conteúdo em vez do formato da mensagem.
    /// </para>
    /// </remarks>
    private static bool IsIngestable(GraphMessage message)
        => !string.IsNullOrEmpty(message.Id) && message.Removed is null;

    private bool IsCandidateAttachment(GraphAttachment attachment)
    {
        if (string.IsNullOrEmpty(attachment.Id))
            return false;

        // Logotipo e assinatura são o que mais aparece numa caixa, e nunca são boleto.
        if (attachment.IsInline == true)
            return false;

        if (attachment.Size is > 0 && attachment.Size > _options.MaxAttachmentBytes)
            return false;

        var contentType = attachment.ContentType?.Split(';')[0].Trim();

        return !string.IsNullOrEmpty(contentType)
            && _options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    /// <remarks>
    /// <strong>Sem <c>$top</c>, de propósito.</strong> Medido em 2026-08-11 contra uma caixa
    /// real: a delta query o ignora e devolve o tamanho de página dela. Quem manda é o header
    /// <c>Prefer: odata.maxpagesize</c>, aplicado em <c>GraphHttp.GetAsync</c>.
    /// </remarks>
    private string InitialDeltaUrl(string mailboxAddress, string folder)
        => $"{Base}users/{Escape(mailboxAddress)}/mailFolders/{folder}/messages/delta"
            + "?$select=id,subject,receivedDateTime,hasAttachments,from,body";

    private string Base => _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";

    private static string Escape(string value) => Uri.EscapeDataString(value);

    private static bool IsAbsoluteHttpUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    private static MailboxAccessProbe Probe(GraphFailure failure, DateTimeOffset now)
        => failure.Status == MailboxStatus.Denied
            ? MailboxAccessProbe.Denied(failure.ReasonCode, failure.Message, now)
            : MailboxAccessProbe.Unavailable(failure.ReasonCode, failure.Message, now);

    private static MailboxReadResult Read(GraphFailure failure, DateTimeOffset now)
    {
        if (failure.Status == MailboxStatus.Denied)
            return MailboxReadResult.Denied(failure.ReasonCode, failure.Message, now);

        return failure.Status == MailboxStatus.CursorExpired
            ? MailboxReadResult.CursorExpired(failure.ReasonCode, failure.Message, now)
            : MailboxReadResult.Unavailable(failure.ReasonCode, failure.Message, now);
    }
}
