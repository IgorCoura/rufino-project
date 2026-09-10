namespace BillPayment.Infra.Extraction;

using System.Net;
using System.Text.RegularExpressions;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Services;

/// <summary>
/// Colhe os endereços de um corpo de mensagem ou de uma página, já reduzidos ao que seria de fato
/// visitado, e ordenados por chance de serem o documento.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Duas passadas, porque uma só perde o caso real.</strong> A versão anterior lia apenas
/// <c>&lt;a href&gt;</c> e por isso não enxergava o boleto da Acessórias, que chega assim:
/// <c>document.write("&lt;iframe src='https://…s3…/arquivo.pdf'&gt;")</c> — nenhuma âncora na
/// página inteira. A passada estruturada cobre as tags que carregam documento; a bruta varre o
/// resto do texto, inclusive dentro de <c>&lt;script&gt;</c>, que é onde aquele endereço mora.
/// </para>
/// <para>
/// <strong>Nenhum padrão aqui tem quantificador aninhado, e isso é decisão de segurança.</strong>
/// O regex de âncora anterior casava <c>[^>]*?href…[^>]*></c> seguido de <c>.*?</c>, que
/// retrocede em O(L²) sobre HTML construído de propósito — negação de serviço com um e-mail só.
/// As tags são achadas por um padrão linear, os atributos são lidos do texto curto da própria
/// tag, e o rótulo da âncora sai por <c>IndexOf</c>, sem regex nenhum.
/// </para>
/// <para>
/// <strong>A ordem passou a importar mais que o conteúdo.</strong> Enquanto havia receita, ela
/// escolhia qual link era o boleto; no regime aberto não há quem escolha, e gastar o orçamento
/// nos oito rastreadores de rede social que a EDP põe antes da fatura significa não achar a
/// fatura. Por isso o que parece documento sai na frente.
/// </para>
/// </remarks>
internal static partial class HtmlLinkHarvester
{
    /// <summary>
    /// Teto de endereços por documento varrido. Um e-mail de campanha traz dezenas e uma página
    /// traz centenas; o teto é o que impede um conteúdo hostil de virar laço na varredura.
    /// </summary>
    private const int MAX_LINKS = 150;

    /// <summary>
    /// Pedaços que aparecem no caminho ou no rótulo de um link de documento, e quase nunca no de
    /// um rastreador.
    /// </summary>
    /// <remarks>
    /// <strong>É ordenação, jamais filtro.</strong> Link que não casa nada aqui continua na lista,
    /// só que atrás — o e-mail medido cujo assunto era "Sua fatura chegou" é o lembrete de que
    /// palavra-chave decide esforço e nunca descarte.
    /// </remarks>
    private static readonly string[] DocumentHints =
    [
        ".pdf", "boleto", "fatura", "guia", "getguia", "2via", "2-via", "segundavia", "segunda-via",
        "cobranca", "cobranç", "invoice", "documento", "download", "danfe", "duplicata", "arquivo",
    ];

    /// <summary>
    /// Colhe do corpo de um e-mail: sem <c>&lt;img&gt;</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Imagem de corpo de e-mail é pixel de rastreio.</strong> Buscá-la gasta orçamento
    /// para entregar ao remetente a confirmação de que a mensagem foi processada, com o IP do
    /// servidor junto — que é reconhecimento gratuito para quem está sondando a caixa.
    /// </remarks>
    public static IReadOnlyList<DocumentLink> Harvest(string? html)
        => Harvest(html, includeImages: false);

    /// <summary>
    /// Colhe de uma página já buscada, onde <c>&lt;img&gt;</c> pode ser o boleto digitalizado.
    /// </summary>
    public static IReadOnlyList<DocumentLink> HarvestFromPage(string? html)
        => Harvest(html, includeImages: true);

    private static IReadOnlyList<DocumentLink> Harvest(string? html, bool includeImages)
    {
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var source = html.Length > HtmlText.MAX_INPUT_LENGTH ? html[..HtmlText.MAX_INPUT_LENGTH] : html;

        var found = new List<Candidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            CollectFromTags(source, includeImages, found, seen);

            // A passada bruta varre o texto inteiro, então ela reencontraria o endereço do
            // `<img>` que a estruturada acabou de recusar — anulando a regra do pixel de rastreio
            // por uma porta lateral. Apagar as tags antes é o que mantém a promessa de pé.
            CollectFromText(includeImages ? source : ImgTag().Replace(source, " "), found, seen);
        }
        catch (RegexMatchTimeoutException)
        {
            // HTML patológico: o timeout existe para isto. O que já foi colhido continua valendo —
            // antes a exceção subia e contava tentativa contra o item.
        }

        return [.. found
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Position)
            .Select(c => c.Link)];
    }

    /// <summary>
    /// Passada estruturada: as tags que podem apontar para um documento.
    /// </summary>
    private static void CollectFromTags(
        string html,
        bool includeImages,
        List<Candidate> found,
        HashSet<string> seen)
    {
        // A projeção carrega Index e Length junto dos grupos: o rótulo da âncora é lido a partir do
        // fim da tag de abertura, e sem a posição não haveria de onde começar a varredura.
        foreach (var (groups, index, length) in DocumentTag().Matches(html).Select(m => (m.Groups, m.Index, m.Length)))
        {
            if (found.Count >= MAX_LINKS)
                return;

            var tagName = groups["tag"].Value.ToLowerInvariant();

            if (tagName == "img" && !includeImages)
                continue;

            var attributes = groups["attrs"].Value;

            var href = tagName switch
            {
                "a" or "area" => AttributeOf(attributes, "href"),
                "object" => AttributeOf(attributes, "data"),
                "meta" => RefreshTargetOf(attributes),
                _ => AttributeOf(attributes, "src"),
            };

            if (string.IsNullOrEmpty(href))
                continue;

            var label = tagName == "a" ? LabelAfter(html, index + length) : null;

            Add(found, seen, href, label, OriginScoreOf(tagName), index);
        }
    }

    /// <summary>
    /// Passada bruta: qualquer endereço absoluto no texto, inclusive dentro de <c>&lt;script&gt;</c>.
    /// </summary>
    /// <remarks>
    /// <strong>É esta que resolve o caso Acessórias</strong>, e ela existe porque a alternativa —
    /// executar o JavaScript num navegador sem cabeça para ver o que a página monta — significaria
    /// rodar código escolhido por quem manda o e-mail dentro da nossa rede. Ler o endereço com um
    /// padrão é estritamente mais seguro, e resolveu os casos medidos.
    /// </remarks>
    private static void CollectFromText(string html, List<Candidate> found, HashSet<string> seen)
    {
        foreach (var (value, index) in AbsoluteUrl().Matches(html).Select(m => (m.Value, m.Index)))
        {
            if (found.Count >= MAX_LINKS)
                return;

            Add(found, seen, value, label: null, originScore: 0, position: index);
        }
    }

    private static void Add(
        List<Candidate> found,
        HashSet<string> seen,
        string rawHref,
        string? label,
        int originScore,
        int position)
    {
        var href = TrimTrailingPunctuation(WebUtility.HtmlDecode(rawHref).Trim());

        if (href.Length == 0)
            return;

        var (target, wasWrapped) = LinkUnwrapService.Unwrap(href);
        var link = DocumentLink.TryCreate(target, label, wasWrapped);

        if (link is null || !seen.Add(link.Url))
            return;

        found.Add(new Candidate(link, originScore + DocumentScoreOf(link), position));
    }

    /// <summary>
    /// Quanto a tag de origem sugere documento, antes de olhar o endereço.
    /// </summary>
    /// <remarks>
    /// <c>iframe</c> e <c>embed</c> ganham da âncora porque é assim que se embute um PDF numa
    /// página; a âncora ganha do texto cru porque endereço solto no meio do HTML é, na maioria das
    /// vezes, script de analytics.
    /// </remarks>
    private static int OriginScoreOf(string tagName) => tagName switch
    {
        "iframe" or "embed" or "object" => 3,
        "a" or "area" => 2,
        "meta" => 2,
        "img" => 1,
        _ => 0,
    };

    private static int DocumentScoreOf(DocumentLink link)
    {
        var score = 0;

        if (link.PathAndQuery.EndsWith(".pdf", StringComparison.Ordinal))
            score += 10;

        if (Array.Exists(DocumentHints, h => link.PathAndQuery.Contains(h, StringComparison.Ordinal)))
            score += 5;

        if (link.Label is { Length: > 0 } label
            && Array.Exists(DocumentHints, h => label.Contains(h, StringComparison.OrdinalIgnoreCase)))
        {
            score += 4;
        }

        // "Acessar Boleto" e "Abrir fatura" são sinal barato e foi o que distinguiu o botão do
        // documento do resto em todos os e-mails medidos.
        if (link.Label is { Length: > 0 } text && BillingSignal.IsPresentIn(text))
            score += 3;

        return score;
    }

    /// <summary>
    /// O texto visível da âncora, lido por varredura simples a partir do fim da tag de abertura.
    /// </summary>
    /// <remarks>
    /// <strong>Sem regex de propósito.</strong> O <c>(?&lt;text&gt;.*?)(?:&lt;/a&gt;|$)</c> da
    /// versão anterior é justamente o quantificador preguiçoso que retrocede sobre HTML hostil.
    /// Um <c>IndexOf</c> com teto faz a mesma coisa em tempo linear e sem poder estourar.
    /// </remarks>
    private static string? LabelAfter(string html, int start)
    {
        if (start >= html.Length)
            return null;

        const int MAX_LABEL_SCAN = 500;

        var limit = Math.Min(html.Length, start + MAX_LABEL_SCAN);
        var close = html.IndexOf("</a", start, limit - start, StringComparison.OrdinalIgnoreCase);
        var end = close < 0 ? limit : close;
        var inner = html[start..end];

        var text = HtmlText.ToPlainText(inner).Trim();

        return string.IsNullOrEmpty(text) ? null : Whitespace().Replace(text, " ");
    }

    /// <summary>Lê um atributo do texto da própria tag, que é curto por construção.</summary>
    private static string? AttributeOf(string attributes, string name)
    {
        foreach (var groups in Attribute().Matches(attributes).Select(m => m.Groups))
        {
            if (!string.Equals(groups["name"].Value, name, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = groups["quoted"].Success
                ? groups["quoted"].Value
                : groups["bare"].Value;

            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    /// <summary>
    /// O destino de <c>&lt;meta http-equiv="refresh" content="0;url=…"&gt;</c>.
    /// </summary>
    /// <remarks>
    /// É redirecionamento feito no HTML em vez de no cabeçalho — o mesmo desvio que o
    /// <c>AllowAutoRedirect = false</c> recusa no <c>302</c>. Aqui ele não é seguido às cegas: o
    /// endereço vira mais um candidato, sujeito às mesmas travas de faixa, porta e orçamento.
    /// </remarks>
    private static string? RefreshTargetOf(string attributes)
    {
        var equiv = AttributeOf(attributes, "http-equiv");

        if (!string.Equals(equiv?.Trim(), "refresh", StringComparison.OrdinalIgnoreCase))
            return null;

        var content = AttributeOf(attributes, "content");

        if (string.IsNullOrEmpty(content))
            return null;

        var marker = content.IndexOf("url=", StringComparison.OrdinalIgnoreCase);

        return marker < 0 ? null : content[(marker + 4)..].Trim().Trim('\'', '"');
    }

    /// <summary>
    /// Apara a pontuação que o texto ao redor cola no fim do endereço.
    /// </summary>
    /// <remarks>
    /// A passada bruta pega <c>…/boleto.pdf".</c> dentro de uma string de JavaScript ou
    /// <c>(https://…/guia)</c> no meio de uma frase. Sem aparar, a URL vai para a rede com lixo
    /// no fim e responde 404 — que se parece com "documento não existe" e mandaria o item para a
    /// quarentena por um motivo inventado.
    /// </remarks>
    private static string TrimTrailingPunctuation(string url)
        => url.TrimEnd('.', ',', ';', ':', ')', ']', '}', '"', '\'', '>', '\\');

    private readonly record struct Candidate(DocumentLink Link, int Score, int Position);

    /// <remarks>
    /// <c>[^&gt;]*</c> é ganancioso e simples — sem alternativa aninhada, não há como retroceder.
    /// A tag de fechamento é excluída por <c>(?!/)</c> para não colher <c>&lt;/a&gt;</c>.
    /// </remarks>
    [GeneratedRegex(
        @"<(?<tag>area|a|iframe|frame|embed|object|meta|img)\b(?<attrs>[^>]*)>",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        2000)]
    private static partial Regex DocumentTag();

    [GeneratedRegex(
        """(?<name>[A-Za-z_:][-A-Za-z0-9_:.]*)\s*=\s*(?:"(?<quoted>[^"]*)"|'(?<quoted>[^']*)'|(?<bare>[^\s"'>]+))""",
        RegexOptions.ExplicitCapture,
        2000)]
    private static partial Regex Attribute();

    /// <summary>
    /// Endereço absoluto solto no texto.
    /// </summary>
    /// <remarks>
    /// Classe negada de caractere único, com teto — linear por construção, e por isso
    /// <c>NonBacktracking</c> aceita compilá-lo. É a passada que enxerga o endereço dentro do
    /// <c>document.write</c>.
    /// </remarks>
    [GeneratedRegex(
        """https?://[^\s"'<>(){}\[\]\\^`|]{1,2000}""",
        RegexOptions.IgnoreCase | RegexOptions.NonBacktracking,
        2000)]
    private static partial Regex AbsoluteUrl();

    [GeneratedRegex(@"\s+", RegexOptions.None, 2000)]
    private static partial Regex Whitespace();

    /// <remarks>
    /// Classe negada simples, sem alternativa aninhada — linear, como os outros padrões daqui.
    /// </remarks>
    [GeneratedRegex(@"<img\b[^>]*>", RegexOptions.IgnoreCase, 2000)]
    private static partial Regex ImgTag();
}
