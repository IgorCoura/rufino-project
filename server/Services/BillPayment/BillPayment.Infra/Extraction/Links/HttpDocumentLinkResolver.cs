namespace BillPayment.Infra.Extraction.Links;

using System.Net;
using System.Text;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Busca o documento apontado pelo corpo da mensagem, descendo a escada em largura.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Este é o único ponto do BC que busca um endereço vindo de fora</strong>, e por isso
/// carrega travas que não podem erodir, nenhuma substituível pelas outras: endereço IP conferido
/// e <em>pinado no connect</em>, porta restrita, redirecionamento não seguido, profundidade,
/// orçamento total, teto por host e conjunto de visitados. Só <c>GET</c>: nada aqui envia
/// formulário nem preenche credencial (ADR-012).
/// </para>
/// <para>
/// <strong>Profundidade não é o que segura volume — o orçamento é.</strong> Cinco níveis com
/// sessenta links por página são 60⁵ ≈ 777 milhões de requisições saindo da nossa rede por causa
/// de um e-mail. A profundidade limita a <em>forma</em> da árvore; quem limita o tamanho é
/// <c>MaxFetchesPerMessage</c>, e os dois precisam coexistir.
/// </para>
/// <para>
/// <strong>Nenhuma URL entra no log.</strong> Medido em 2026-08-11: os endereços de boleto
/// respondem <c>200</c> sem autenticação nenhuma. A URL é uma credencial ao portador — logá-la é
/// o mesmo que logar o boleto. O log registra host, nível e desfecho. O cliente HTTP também tem os
/// loggers do <c>IHttpClientFactory</c> removidos no registro, senão eles escreveriam a URI
/// completa por baixo desta regra.
/// </para>
/// </remarks>
internal sealed class HttpDocumentLinkResolver(
    IHttpClientFactory httpClientFactory,
    SenderFetchBudget senderBudget,
    IOptions<LinkResolutionOptions> options,
    ILogger<HttpDocumentLinkResolver> logger) : IDocumentLinkResolver
{
    internal const string CLIENT_NAME = "document-link";

    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();

    private readonly LinkResolutionOptions _options = options.Value;

    public bool IsEnabled => _options.Enabled
        && (_options.Mode == LinkResolutionMode.Open || _options.Recipes.Count > 0);

    public IReadOnlyCollection<string> ResolvableHosts =>
        [.. _options.Recipes.Select(r => r.Host).Where(h => !string.IsNullOrWhiteSpace(h))];

    public async Task<LinkResolution> ResolveAsync(
        ReadOnlyMemory<byte> body,
        string? contentType,
        string? sender,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled || body.IsEmpty)
            return LinkResolution.Disabled();

        var links = HtmlLinkHarvester.Harvest(Encoding.UTF8.GetString(body.Span));

        if (links.Count == 0)
            return LinkResolution.Failed(LinkResolutionOutcome.NoCandidates);

        // O melhor candidato é gravado mesmo quando nada resolve — é ele que transforma a
        // quarentena em fila de emissores a cadastrar.
        var best = links[0];

        if (!senderBudget.TryReserve(sender))
        {
            logger.LogWarning("Teto diário de buscas de link atingido para um remetente; a escada foi pulada.");
            return LinkResolution.Failed(LinkResolutionOutcome.Throttled, best);
        }

        // O teto de tempo da escada inteira: profundidade e orçamento limitam quantas requisições
        // saem, não quanto tempo elas levam.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(_options.TotalTimeoutSeconds));

        try
        {
            return await WalkAsync(links, best, deadline.Token);
        }
        catch (OperationCanceledException cancelled) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                cancelled, "A escada de link esgotou o tempo total antes de achar o documento.");

            return LinkResolution.Failed(LinkResolutionOutcome.Unreachable, best);
        }
    }

    /// <summary>
    /// A escada, em largura: todo o nível 1 antes de qualquer link de nível 2.
    /// </summary>
    /// <remarks>
    /// <strong>Largura e não profundidade, porque o documento está raso.</strong> Nos casos
    /// medidos ele aparece no primeiro ou no segundo salto; descer em profundidade gastaria o
    /// orçamento inteiro num ramo enquanto o boleto espera no link seguinte do e-mail.
    /// </remarks>
    private async Task<LinkResolution> WalkAsync(
        IReadOnlyList<DocumentLink> links,
        DocumentLink best,
        CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient(CLIENT_NAME);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var perHost = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var frontier = SeedFrom(links);
        var seeded = frontier.Count;

        var budget = _options.MaxFetchesPerMessage;
        var fetches = 0;
        var refused = 0;
        var deepest = 0;

        for (var depth = 1; depth <= _options.MaxDepth && frontier.Count > 0; depth++)
        {
            var next = new List<Step>();
            deepest = depth;

            foreach (var step in frontier)
            {
                if (budget <= 0)
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "A escada gastou as {Budget} requisições permitidas no nível {Depth} sem achar o documento.",
                            _options.MaxFetchesPerMessage, depth);
                    }

                    return LinkResolution.Failed(LinkResolutionOutcome.BudgetExhausted, best, fetches, deepest);
                }

                if (!visited.Add(step.Link.Url) || !IsReachable(step, perHost))
                {
                    refused++;
                    continue;
                }

                budget--;
                fetches++;
                perHost[step.Link.Host] = perHost.GetValueOrDefault(step.Link.Host) + 1;

                var fetched = await FetchAsync(http, step.Link, depth, cancellationToken);

                if (fetched is null)
                    continue;

                if (AsDocument(fetched, step.Link.Url) is { } document)
                    return LinkResolution.Resolved(document, step.Root, fetches, depth);

                if (depth < _options.MaxDepth && HtmlText.LooksLikeHtml(fetched.Content.Span))
                    next.AddRange(ChildrenOf(fetched, step));
            }

            frontier = next;
        }

        return LinkResolution.Failed(OutcomeOf(seeded, fetches, refused, deepest), best, fetches, deepest);
    }

    /// <summary>
    /// Qual desfecho descreve o que aconteceu — a diferença entre "cadastre o emissor" e
    /// "conserte alguma coisa".
    /// </summary>
    private LinkResolutionOutcome OutcomeOf(int seeded, int fetches, int refused, int deepest)
    {
        if (seeded == 0)
        {
            // Havia link no corpo e nenhum entrou na escada. No regime fechado isso é falta de
            // receita — a fila de trabalho; no aberto, é recusa das travas de rede.
            return _options.Mode == LinkResolutionMode.Allowlist
                ? LinkResolutionOutcome.NoRecipe
                : LinkResolutionOutcome.Refused;
        }

        if (fetches == 0)
            return LinkResolutionOutcome.Refused;

        // Chegou ao fundo com candidatos ainda aparecendo: o documento pode estar além do teto.
        // Diferente de ter percorrido tudo e os endereços não terem entregado nada.
        if (deepest >= _options.MaxDepth)
            return LinkResolutionOutcome.DepthExhausted;

        return refused > 0 ? LinkResolutionOutcome.Refused : LinkResolutionOutcome.Unreachable;
    }

    /// <summary>
    /// O nível 1: no regime fechado, só o que casa receita; no aberto, tudo — na ordem em que o
    /// colhedor os classificou.
    /// </summary>
    private List<Step> SeedFrom(IReadOnlyList<DocumentLink> links)
    {
        if (_options.Mode == LinkResolutionMode.Open)
            return [.. links.Select(l => new Step(l, Recipe: null, Root: l))];

        var seeds = new List<Step>();

        // Documento direto primeiro: quando existe, resolve com uma requisição só e evita
        // percorrer uma página intermediária à toa.
        foreach (var link in links)
        {
            var recipe = RecipeFor(link);

            if (recipe is not null)
                seeds.Add(new Step(link, recipe, Root: link));
        }

        return [.. seeds.OrderByDescending(s => s.Recipe!.DirectDocument)];
    }

    private LinkRecipe? RecipeFor(DocumentLink link) => _options.Recipes.FirstOrDefault(r =>
        string.Equals(r.Host, link.Host, StringComparison.OrdinalIgnoreCase)
        && r.Port == link.Port
        && (string.IsNullOrEmpty(r.PathPrefix)
            || link.PathAndQuery.StartsWith(r.PathPrefix, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Os candidatos que uma página buscada oferece para o nível seguinte.
    /// </summary>
    /// <remarks>
    /// <strong>No regime fechado a lista de hosts da receita continua mandando</strong> — sem ela,
    /// o conteúdo de uma página de terceiro passaria a decidir o que a nossa rede requisita, que é
    /// o mesmo buraco que a allowlist fecha na entrada, reaberto pela porta dos fundos. No aberto
    /// não há lista a respeitar, e quem segura é a faixa de IP conferida no <em>connect</em>.
    /// </remarks>
    private static IEnumerable<Step> ChildrenOf(FetchedContent page, Step parent)
    {
        var html = Encoding.UTF8.GetString(page.Content.Span);
        var children = HtmlLinkHarvester.HarvestFromPage(html);

        if (parent.Recipe is null)
            return children.Select(c => new Step(c, Recipe: null, parent.Root));

        var allowed = parent.Recipe.FollowHosts.Count > 0
            ? parent.Recipe.FollowHosts
            : [parent.Recipe.Host];

        return children
            .Where(c => allowed.Contains(c.Host, StringComparer.OrdinalIgnoreCase))
            .Select(c => new Step(c, parent.Recipe, parent.Root));
    }

    /// <summary>
    /// As travas que não dependem de tocar a rede: porta, host recusado e teto por host.
    /// </summary>
    private bool IsReachable(Step step, Dictionary<string, int> perHost)
    {
        if (perHost.GetValueOrDefault(step.Link.Host) >= _options.MaxFetchesPerHost)
            return false;

        if (IsBlockedHost(step.Link.Host))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "{Host} está na lista de hosts recusados; o documento não foi buscado.", step.Link.Host);
            }

            return false;
        }

        // A receita É a autorização da porta — é assim que o PDF da SABESP em :7446 continua
        // alcançável sem afrouxar a lista genérica.
        if (step.Recipe is not null && step.Recipe.Port == step.Link.Port)
            return true;

        if (!_options.AllowedPorts.Contains(step.Link.Port))
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Porta {Port} recusada em {Host}: fora da lista permitida.", step.Link.Port, step.Link.Host);
            }

            return false;
        }

        return true;
    }

    private bool IsBlockedHost(string host) => _options.BlockedHosts.Any(blocked =>
        !string.IsNullOrWhiteSpace(blocked)
        && (host.Equals(blocked, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + blocked.TrimStart('.'), StringComparison.OrdinalIgnoreCase)));

    private sealed record FetchedContent(ReadOnlyMemory<byte> Content, string? MediaType);

    /// <summary>
    /// Um endereço na fila da escada, com a receita que o governa e a raiz do ramo.
    /// </summary>
    /// <remarks>
    /// <strong>A raiz viaja junto porque é ela que serve de procedência.</strong> O endereço que
    /// de fato entrega os bytes costuma ser efêmero — o da Acessórias é presignado com
    /// <c>X-Amz-Expires=120</c> e morre em dois minutos. Gravar esse como "de onde veio o
    /// documento" produziria uma evidência que já está morta quando alguém abre a quarentena; o
    /// que se guarda é o link do e-mail, que é estável e reabre o caminho inteiro.
    /// </remarks>
    private readonly record struct Step(DocumentLink Link, LinkRecipe? Recipe, DocumentLink Root);

    private async Task<FetchedContent?> FetchAsync(
        HttpClient http,
        DocumentLink link,
        int depth,
        CancellationToken cancellationToken)
    {
        // O host sai para uma variável porque é a ÚNICA parte da URL que pode ser logada: o resto
        // do endereço é credencial ao portador.
        var host = link.Host;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(link.Url));
            using var response = await http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // Redirecionamento é o jeito mais simples de burlar a allowlist: o host autorizado
            // responde e manda o cliente para outro lugar. Conta como não encontrado.
            if (IsRedirect(response.StatusCode))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "{Host} respondeu com redirecionamento no nível {Depth}; o documento não foi buscado.",
                        host, depth);
                }

                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "{Host} respondeu {Status} ao pedido do documento no nível {Depth}.",
                        host, (int)response.StatusCode, depth);
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
        catch (Exception ex) when (ex is HttpRequestException or UriFormatException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(ex, "Não foi possível buscar o documento em {Host}.", host);

            return null;
        }
    }

    /// <summary>
    /// Lê no máximo o teto configurado, com prazo próprio.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O cabeçalho de tamanho é do outro lado, e pode mentir</strong> — por isso o teto é
    /// conferido nos bytes lidos, não no <c>Content-Length</c>.
    /// </para>
    /// <para>
    /// <strong>E o prazo é próprio porque o <c>HttpClient.Timeout</c> não cobre esta parte.</strong>
    /// Com <c>ResponseHeadersRead</c> ele termina nos cabeçalhos e solta a leitura do stream: um
    /// servidor que responde 200 e entrega um byte por segundo seguraria o worker para sempre, e
    /// do lado de fora isso não se distingue de um host lento honesto.
    /// </para>
    /// </remarks>
    private async Task<ReadOnlyMemory<byte>?> ReadCappedAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readTimeout.CancelAfter(TimeSpan.FromSeconds(_options.ReadTimeoutSeconds));

        await using var stream = await response.Content.ReadAsStreamAsync(readTimeout.Token);
        using var buffer = new MemoryStream();

        var chunk = new byte[81_920];
        int read;

        while ((read = await stream.ReadAsync(chunk, readTimeout.Token)) > 0)
        {
            if (buffer.Length + read > _options.MaxBytes)
                return null;

            await buffer.WriteAsync(chunk.AsMemory(0, read), readTimeout.Token);
        }

        return buffer.Length == 0 ? null : buffer.ToArray();
    }

    /// <summary>
    /// Aceita como documento só o que a cascata sabe abrir, <strong>conferido nos bytes</strong>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A imagem passou a ser reconhecida pela assinatura, como o PDF sempre foi.</strong>
    /// Antes ela era aceita pelo <c>Content-Type</c> que o servidor remoto declarava — e quem
    /// serve o arquivo está fora do nosso controle. Um servidor hostil respondendo
    /// <c>image/png</c> com vinte megabytes de qualquer coisa entregava esses bytes ao
    /// decodificador de imagem do leitor de QR, ao extrator de IA e ao aparelho de quem aprova.
    /// </para>
    /// <para>
    /// O PDF nunca dependeu do cabeçalho, e pelo mesmo motivo: a SABESP entrega o dela numa porta
    /// não-padrão, e o próprio Graph rotula anexo de boleto como <c>application/octet-stream</c>.
    /// </para>
    /// </remarks>
    private static ResolvedDocument? AsDocument(FetchedContent fetched, string url)
    {
        var span = fetched.Content.Span;

        if (span.StartsWith(PdfMagic))
            return ResolvedDocument.From(fetched.Content, DocumentPayload.PDF, url);

        var mediaType = ImageMagic.MediaTypeOf(span);

        return mediaType is null ? null : ResolvedDocument.From(fetched.Content, mediaType, url);
    }

    private static bool IsRedirect(HttpStatusCode status)
        => status is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
}

/// <summary>
/// O resolvedor que não resolve nada — entra quando a escada está desligada.
/// </summary>
/// <remarks>
/// <strong>Degradação, não falha.</strong> Sem escada, a cascata termina no corpo do e-mail e o
/// que não resolveu vai para a quarentena, exatamente como antes da 2.5. É o mesmo desenho do
/// extrator de visão — e o oposto do armazenamento, cuja ausência perderia um comprovante que
/// ninguém recupera.
/// </remarks>
internal sealed class NullDocumentLinkResolver : IDocumentLinkResolver
{
    public bool IsEnabled => false;

    public IReadOnlyCollection<string> ResolvableHosts => [];

    public Task<LinkResolution> ResolveAsync(
        ReadOnlyMemory<byte> body,
        string? contentType,
        string? sender,
        CancellationToken cancellationToken)
        => Task.FromResult(LinkResolution.Disabled());
}
