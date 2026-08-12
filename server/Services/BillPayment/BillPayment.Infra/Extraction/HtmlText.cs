namespace BillPayment.Infra.Extraction;

using System.Net;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Reduz o corpo de uma mensagem a texto, preservando o que a cascata precisa achar nele.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A distinção entre tag de bloco e tag inline não é cosmética.</strong> O varredor de
/// candidatos trata quebra de linha como fim de sequência de dígitos — regra que existe para não
/// emendar números de linhas diferentes e produzir um código que não está no documento. Se toda
/// tag virasse quebra, uma linha digitável partida em <c>&lt;span&gt;</c>s (que é como muito
/// e-mail de cobrança é montado) seria cortada ao meio e nunca fecharia o DV. Se nenhuma virasse,
/// células vizinhas de uma tabela se emendariam e criariam candidatos inexistentes.
/// </para>
/// <para>
/// <strong>Nada aqui é logado.</strong> O texto do corpo carrega a linha digitável e o BR Code, que
/// são instrumento de pagamento: quem os tem, paga.
/// </para>
/// </remarks>
internal static partial class HtmlText
{
    /// <summary>
    /// Teto do que se converte. Corpo maior que isto não é e-mail de cobrança, e varrê-lo inteiro
    /// só gastaria memória do worker.
    /// </summary>
    private const int MAX_INPUT_LENGTH = 2 * 1024 * 1024;

    /// <summary>
    /// Converte HTML em texto. Entrada que já é texto puro volta como está.
    /// </summary>
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var source = html.Length > MAX_INPUT_LENGTH ? html[..MAX_INPUT_LENGTH] : html;

        // Script e estilo carregam chaves, colchetes e números que virariam candidatos à toa.
        source = ScriptOrStyleBlock().Replace(source, " ");
        source = BlockLevelTag().Replace(source, "\n");
        source = AnyTag().Replace(source, string.Empty);

        return WebUtility.HtmlDecode(source);
    }

    /// <summary>
    /// Descobre se o conteúdo é HTML, olhando os bytes em vez de acreditar no cabeçalho.
    /// </summary>
    public static bool LooksLikeHtml(ReadOnlySpan<byte> content)
    {
        var probe = Encoding.UTF8.GetString(content[..Math.Min(content.Length, 512)]);

        return probe.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || probe.Contains("<!doctype html", StringComparison.OrdinalIgnoreCase)
            || probe.Contains("<body", StringComparison.OrdinalIgnoreCase)
            || probe.Contains("<table", StringComparison.OrdinalIgnoreCase)
            || probe.Contains("<div", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex ScriptOrStyleBlock();

    [GeneratedRegex(
        @"</?(br|p|div|tr|td|th|table|tbody|thead|li|ul|ol|h[1-6]|section|article|header|footer|blockquote|hr)\b[^>]*>",
        RegexOptions.IgnoreCase,
        2000)]
    private static partial Regex BlockLevelTag();

    [GeneratedRegex("<[^>]+>", RegexOptions.None, 2000)]
    private static partial Regex AnyTag();
}
