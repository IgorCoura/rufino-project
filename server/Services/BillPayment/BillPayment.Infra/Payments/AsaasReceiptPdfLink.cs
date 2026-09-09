namespace BillPayment.Infra.Payments;

using System.Net;
using System.Text.RegularExpressions;

/// <summary>
/// Acha, na página do comprovante, o link para o PDF do próprio provedor.
/// </summary>
/// <remarks>
/// <para>
/// <strong><c>transactionReceiptUrl</c> não é o comprovante — é a página dele.</strong> Medido em
/// 2026-09-09 num comprovante Pix real: a URL responde <c>200</c> sem autenticação, com
/// <c>text/html</c>, e o arquivo de verdade está numa âncora
/// (<c>&lt;a href="/transactionReceipt/pdf/{id}"&gt;Baixar pdf&lt;/a&gt;</c>) que devolve
/// <c>application/pdf</c> — A4 retrato (<c>MediaBox [0 0 595 842]</c>), <strong>uma página</strong>,
/// 27 KB, texto vetorial. Guardar a página no lugar do arquivo é o que deixava a tela do usuário
/// vazia.
/// </para>
/// <para>
/// <strong>Não dá para reusar o <c>HtmlLinkHarvester</c>.</strong> Ele devolve
/// <c>DocumentLink</c>, que exige endereço <em>absoluto</em>, e o <c>href</c> do comprovante é
/// relativo. O que se aproveita dele é a disciplina: regex de âncora com timeout, teto de links, e
/// <c>RegexMatchTimeoutException</c> tratada como "sem link" em vez de exceção.
/// </para>
/// <para>
/// <strong>A escolha é pelo formato do caminho, nunca pela ordem na página.</strong>
/// <c>/pdf/{id}</c> é a convenção do provedor para arquivo — o boleto emitido usa a mesma
/// (<c>bankSlipUrl</c> = <c>/b/pdf/{id}</c>, documentado). "Pegar o primeiro link" erraria: a
/// página tem CSS, script e ícone antes do botão.
/// </para>
/// <para>
/// <strong>Mesmo host e mesma porta são a trava de segurança.</strong> O HTML vem de fora, e um
/// <c>href</c> para outro endereço faria a aplicação buscar onde a página mandasse. Exigir o host
/// da própria página — que já passou pela <c>SafeUrlPolicy</c> antes da primeira requisição —
/// fecha o SSRF sem precisar de uma segunda política.
/// </para>
/// </remarks>
internal static partial class AsaasReceiptPdfLink
{
    /// <summary>
    /// Teto de âncoras examinadas. A página do comprovante tem um punhado; o teto é o que impede
    /// um HTML hostil de virar laço.
    /// </summary>
    private const int MAX_ANCHORS = 60;

    /// <summary>
    /// O endereço do PDF, absoluto, ou <c>null</c> quando a página não oferece um. "Não achei" é
    /// desfecho normal — quem chama fica com a página, como antes.
    /// </summary>
    public static Uri? TryResolve(string html, Uri pageUri)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        try
        {
            var examined = 0;

            foreach (Match match in Anchor().Matches(html))
            {
                if (++examined > MAX_ANCHORS)
                    break;

                var href = WebUtility.HtmlDecode(match.Groups["href"].Value);

                if (string.IsNullOrWhiteSpace(href))
                    continue;

                // Resolve relativo E absoluto contra a página, num passo só.
                if (!Uri.TryCreate(pageUri, href.Trim(), out var candidate))
                    continue;

                if (candidate.Scheme != Uri.UriSchemeHttps && candidate.Scheme != Uri.UriSchemeHttp)
                    continue;

                if (!string.Equals(candidate.Host, pageUri.Host, StringComparison.OrdinalIgnoreCase)
                    || candidate.Port != pageUri.Port)
                {
                    continue;
                }

                if (LooksLikeDocument(candidate.AbsolutePath))
                    return candidate;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // HTML patológico: o timeout existe para isto. "Sem link" é o desfecho certo — a
            // página segue sendo guardada, e nenhuma tentativa é gasta contra a ordem.
        }

        return null;
    }

    /// <summary><c>.../pdf/{id}</c> ou um caminho que termina em <c>.pdf</c>.</summary>
    private static bool LooksLikeDocument(string absolutePath)
        => DocumentPath().IsMatch(absolutePath);

    /// <remarks>
    /// Mesmo padrão de âncora do <c>HtmlLinkHarvester</c>, sem o texto: aqui quem decide é o
    /// caminho, não o rótulo.
    /// </remarks>
    [GeneratedRegex(
        """<a\b[^>]*?href\s*=\s*(?:"(?<href>[^"]*)"|'(?<href>[^']*)')""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        2000)]
    private static partial Regex Anchor();

    [GeneratedRegex(
        @"(?:/pdf/[A-Za-z0-9._~-]+|\.pdf)/?$",
        RegexOptions.IgnoreCase,
        2000)]
    private static partial Regex DocumentPath();
}
