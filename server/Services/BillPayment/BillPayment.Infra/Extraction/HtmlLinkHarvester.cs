namespace BillPayment.Infra.Extraction;

using System.Net;
using System.Text.RegularExpressions;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Services;

/// <summary>
/// Colhe os links de um corpo de mensagem, já reduzidos ao endereço que seria de fato visitado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>"Pegue o primeiro link" erra em todos os casos medidos.</strong> O e-mail da EDP tem
/// oito links de rastreamento de rede social antes do link da fatura; o do condomínio tem um
/// <c>EmailAdvertisingClick</c> logo abaixo do botão do boleto. Por isso a colheita devolve
/// <em>todos</em> os candidatos com o texto da âncora, e quem escolhe é a receita — nunca a ordem.
/// </para>
/// <para>
/// <strong>O texto da âncora vem junto porque é sinal barato.</strong> "Acessar Boleto" e "Abrir
/// fatura" distinguem o documento do resto sem nenhuma chamada de rede.
/// </para>
/// </remarks>
internal static partial class HtmlLinkHarvester
{
    /// <summary>
    /// Teto de links por mensagem. Um e-mail de campanha traz dezenas, e nenhum deles depois do
    /// primeiro punhado é boleto — o teto é o que impede uma mensagem hostil de virar laço.
    /// </summary>
    private const int MAX_LINKS = 60;

    /// <summary>
    /// Extrai os links, desembrulha rastreador e deduplica pelo endereço final.
    /// </summary>
    public static IReadOnlyList<DocumentLink> Harvest(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var found = new List<DocumentLink>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var groups in Anchor().Matches(html).Select(m => m.Groups))
        {
            if (found.Count >= MAX_LINKS)
                break;

            var href = WebUtility.HtmlDecode(groups["href"].Value);
            var (target, wasWrapped) = LinkUnwrapService.Unwrap(href);

            var link = DocumentLink.TryCreate(target, LabelOf(groups["text"].Value), wasWrapped);

            if (link is not null && seen.Add(link.Url))
                found.Add(link);
        }

        return found;
    }

    /// <summary>
    /// O texto visível da âncora, sem a marcação que os e-mails usam dentro do botão.
    /// </summary>
    private static string? LabelOf(string inner)
    {
        if (string.IsNullOrWhiteSpace(inner))
            return null;

        var text = HtmlText.ToPlainText(inner).Trim();

        return string.IsNullOrEmpty(text) ? null : Whitespace().Replace(text, " ");
    }

    /// <remarks>
    /// Casa a âncora com ou sem fechamento: e-mail montado por editor visual costuma deixar
    /// <c>&lt;/a&gt;</c> faltando, e perder o link do boleto por causa disso seria perder a conta.
    /// </remarks>
    [GeneratedRegex(
        """<a\b[^>]*?href\s*=\s*(?:"(?<href>[^"]*)"|'(?<href>[^']*)')[^>]*>(?<text>.*?)(?:</a>|$)""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        2000)]
    private static partial Regex Anchor();

    [GeneratedRegex(@"\s+", RegexOptions.None, 2000)]
    private static partial Regex Whitespace();
}
