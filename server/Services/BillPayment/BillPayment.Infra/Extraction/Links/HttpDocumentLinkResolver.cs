namespace BillPayment.Infra.Extraction.Links;

using System.Net;
using System.Text;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Busca o documento apontado pelo corpo da mensagem, dentro de uma allowlist fechada.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Este é o único ponto do BC que busca um endereço vindo de fora</strong>, e por isso
/// carrega quatro travas que não podem erodir, nenhuma delas substituível pelas outras: allowlist
/// por host <em>e</em> porta, endereço resolvido conferido contra faixa interna, redirecionamento
/// não seguido, e teto de requisições por mensagem. Só <c>GET</c>: nada aqui envia formulário nem
/// preenche credencial (ADR-012).
/// </para>
/// <para>
/// <strong>Nenhuma URL entra no log.</strong> Medido em 2026-08-11: os quatro endereços de boleto
/// respondem <c>200</c> sem autenticação nenhuma. A URL é uma credencial ao portador — logá-la é o
/// mesmo que logar o boleto. O log registra host e desfecho.
/// </para>
/// <para>
/// <strong>Não lança por documento inalcançável.</strong> Link expirado, host fora do ar ou
/// resposta que não é documento devolvem <c>null</c>, e o artefato segue para a quarentena como
/// qualquer outro que a cascata não resolveu.
/// </para>
/// </remarks>
internal sealed class HttpDocumentLinkResolver(
    IHttpClientFactory httpClientFactory,
    IOptions<LinkResolutionOptions> options,
    ILogger<HttpDocumentLinkResolver> logger) : IDocumentLinkResolver
{
    internal const string CLIENT_NAME = "document-link";

    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();

    private static readonly string[] AcceptedImageTypes = ["image/png", "image/jpeg", "image/webp"];

    private readonly LinkResolutionOptions _options = options.Value;

    public bool IsEnabled => _options.Enabled && _options.Recipes.Count > 0;

    public IReadOnlyCollection<string> ResolvableHosts =>
        [.. _options.Recipes.Select(r => r.Host).Where(h => !string.IsNullOrWhiteSpace(h))];

    /// <summary>Colhe e desembrulha os links do corpo. Sem rede, e SEM filtrar por receita.</summary>
    /// <remarks>
    /// A ausência de filtro é o ponto: quem chama quer justamente os hosts que ainda não sabemos
    /// buscar, para transformá-los em fila de receitas a cadastrar.
    /// </remarks>
    public IReadOnlyCollection<DocumentLink> HarvestLinks(ReadOnlyMemory<byte> body, string? contentType)
        => body.IsEmpty ? [] : HtmlLinkHarvester.Harvest(Encoding.UTF8.GetString(body.Span));

    public async Task<ResolvedDocument?> ResolveAsync(
        ReadOnlyMemory<byte> body,
        string? contentType,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled || body.IsEmpty)
            return null;

        var links = HtmlLinkHarvester.Harvest(Encoding.UTF8.GetString(body.Span));

        if (links.Count == 0)
            return null;

        var http = httpClientFactory.CreateClient(CLIENT_NAME);
        var budget = _options.MaxFetchesPerMessage;

        // Documento direto primeiro: quando existe, resolve com uma requisição só e evita
        // percorrer uma página intermediária à toa.
        foreach (var (link, recipe) in Matches(links).OrderByDescending(m => m.Recipe.DirectDocument))
        {
            if (budget <= 0)
                break;

            budget--;
            var fetched = await FetchAsync(http, link.Url, cancellationToken);

            if (fetched is null)
                continue;

            if (AsDocument(fetched, link.Url) is { } document)
                return document;

            if (recipe.DirectDocument)
                continue;

            var hop = await FollowAsync(http, fetched, recipe, link, budget, cancellationToken);

            budget -= hop.Spent;

            if (hop.Document is not null)
                return hop.Document;
        }

        return null;
    }

    /// <summary>
    /// Os links do corpo para os quais existe receita — host, porta e prefixo de caminho.
    /// </summary>
    private IEnumerable<(DocumentLink Link, LinkRecipe Recipe)> Matches(IReadOnlyList<DocumentLink> links)
    {
        foreach (var link in links)
        {
            var recipe = _options.Recipes.FirstOrDefault(r =>
                string.Equals(r.Host, link.Host, StringComparison.OrdinalIgnoreCase)
                && r.Port == link.Port
                && (string.IsNullOrEmpty(r.PathPrefix)
                    || link.PathAndQuery.StartsWith(r.PathPrefix, StringComparison.OrdinalIgnoreCase)));

            if (recipe is not null)
                yield return (link, recipe);
        }
    }

    /// <summary>
    /// O segundo e último salto: procura o documento dentro da página que veio.
    /// </summary>
    /// <remarks>
    /// <strong>Um salto, e só para host declarado na receita.</strong> Sem essa lista, o conteúdo
    /// de uma página de terceiro passaria a decidir o que a nossa rede requisita — que é o mesmo
    /// buraco que a allowlist fecha na entrada, reaberto pela porta dos fundos.
    /// </remarks>
    private async Task<(ResolvedDocument? Document, int Spent)> FollowAsync(
        HttpClient http,
        FetchedContent page,
        LinkRecipe recipe,
        DocumentLink origin,
        int budget,
        CancellationToken cancellationToken)
    {
        if (!HtmlText.LooksLikeHtml(page.Content.Span))
            return (null, 0);

        var allowed = recipe.FollowHosts.Count > 0 ? recipe.FollowHosts : [recipe.Host];
        var spent = 0;

        foreach (var candidate in HtmlLinkHarvester.Harvest(Encoding.UTF8.GetString(page.Content.Span)))
        {
            if (spent >= budget)
                break;

            if (!allowed.Contains(candidate.Host, StringComparer.OrdinalIgnoreCase))
                continue;

            if (string.Equals(candidate.Url, origin.Url, StringComparison.Ordinal))
                continue;

            spent++;
            var fetched = await FetchAsync(http, candidate.Url, cancellationToken);

            if (fetched is not null && AsDocument(fetched, candidate.Url) is { } document)
                return (document, spent);
        }

        return (null, spent);
    }

    private sealed record FetchedContent(ReadOnlyMemory<byte> Content, string? MediaType);

    private async Task<FetchedContent?> FetchAsync(HttpClient http, string url, CancellationToken cancellationToken)
    {
        var uri = new Uri(url);

        // O host sai para uma variável porque é a ÚNICA parte da URL que pode ser logada: o resto
        // do endereço é credencial ao portador.
        var host = uri.Host;

        if (!await SafeUrlPolicy.IsPubliclyRoutableAsync(host, cancellationToken))
        {
            logger.LogWarning(
                "Endereço de documento recusado: {Host} não resolve para um endereço público.", host);

            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // Redirecionamento é o jeito mais simples de burlar allowlist: o host autorizado
            // responde e manda o cliente para outro lugar. Conta como não encontrado.
            if (IsRedirect(response.StatusCode))
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("{Host} respondeu com redirecionamento; o documento não foi buscado.", host);

                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "{Host} respondeu {Status} ao pedido do documento.", host, (int)response.StatusCode);
                }

                return null;
            }

            if (response.Content.Headers.ContentLength > _options.MaxBytes)
                return null;

            var content = await ReadCappedAsync(response, cancellationToken);

            return content is null
                ? null
                : new FetchedContent(content.Value, response.Content.Headers.ContentType?.MediaType);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(ex, "Não foi possível buscar o documento em {Host}.", host);

            return null;
        }
    }

    /// <summary>
    /// Lê no máximo o teto configurado — o cabeçalho de tamanho é do outro lado, e pode mentir.
    /// </summary>
    private async Task<ReadOnlyMemory<byte>?> ReadCappedAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();

        var chunk = new byte[81_920];
        int read;

        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > _options.MaxBytes)
                return null;

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return buffer.Length == 0 ? null : buffer.ToArray();
    }

    /// <summary>
    /// Aceita como documento só o que a cascata sabe abrir.
    /// </summary>
    /// <remarks>
    /// <strong>O PDF é reconhecido pelos bytes, não pelo cabeçalho.</strong> Quem serve o arquivo
    /// está fora do nosso controle e nada obriga o <c>Content-Type</c> a estar certo — a SABESP
    /// entrega o dela numa porta não-padrão, e o próprio Graph rotula anexo de boleto como
    /// <c>application/octet-stream</c>.
    /// </remarks>
    private static ResolvedDocument? AsDocument(FetchedContent fetched, string url)
    {
        var span = fetched.Content.Span;

        if (span.Length >= PdfMagic.Length && span[..PdfMagic.Length].SequenceEqual(PdfMagic))
            return ResolvedDocument.From(fetched.Content, DocumentPayload.PDF, url);

        var mediaType = fetched.MediaType?.Trim().ToLowerInvariant();

        return Array.Exists(AcceptedImageTypes, t => string.Equals(t, mediaType, StringComparison.Ordinal))
            ? ResolvedDocument.From(fetched.Content, mediaType, url)
            : null;
    }

    private static bool IsRedirect(HttpStatusCode status)
        => status is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
}

/// <summary>
/// O resolvedor que não resolve nada — entra quando não há receita configurada.
/// </summary>
/// <remarks>
/// <strong>Degradação, não falha.</strong> Sem receita, a cascata termina no corpo do e-mail e o
/// que não resolveu vai para a quarentena, exatamente como antes da 2.5. É o mesmo desenho do
/// extrator de visão — e o oposto do armazenamento, cuja ausência perderia um comprovante que
/// ninguém recupera.
/// </remarks>
internal sealed class NullDocumentLinkResolver : IDocumentLinkResolver
{
    public bool IsEnabled => false;

    public IReadOnlyCollection<string> ResolvableHosts => [];

    public Task<ResolvedDocument?> ResolveAsync(
        ReadOnlyMemory<byte> body,
        string? contentType,
        CancellationToken cancellationToken)
        => Task.FromResult<ResolvedDocument?>(null);

    // Vazio, e não a colheita de verdade: com a escada desligada não há para onde ir, e registrar
    // um endereço que ninguém buscaria descreveria uma tentativa que não aconteceu.
    public IReadOnlyCollection<DocumentLink> HarvestLinks(ReadOnlyMemory<byte> body, string? contentType) => [];
}
