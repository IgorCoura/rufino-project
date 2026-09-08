namespace BillPayment.Domain.SharedKernel;

using System.Globalization;
using System.Text;

/// <summary>
/// A normalização de nome de pessoa jurídica que o BC inteiro usa para comparar razão social,
/// nome fantasia e apelidos.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque havia duas.</strong> Até 2026-09-08 o cadastro comparava nomes com
/// <c>Trim()</c> e nada mais, enquanto o serviço que detecta sósia — na classe ao lado — já
/// derrubava acento, pontuação e caixa. A regra frouxa estava no caminho que só levanta
/// suspeita; a estrita, no caminho que decide. O efeito era o alarme falso crônico relatado
/// pelo usuário: <c>"EDP SÃO PAULO ... S.A."</c> da consulta oficial não casava com
/// <c>"EDP SAO PAULO ... S/A"</c> do cadastro, e a divergência de grafia virava evidência.
/// </para>
/// <para>
/// <strong>Tolerância de grafia, nunca de semelhança.</strong> Depois de normalizar, a
/// comparação continua sendo igualdade exata: dois nomes diferentes nunca passam a casar. É o
/// que mantém o cotejo de sósia — nome conhecido com documento fiscal de outra pessoa — valendo
/// exatamente o que valia.
/// </para>
/// </remarks>
public static class PartyName
{
    /// <summary>
    /// Reduz o nome ao que sobrevive à variação de emissor: sem acento, sem pontuação, sem
    /// espaço e em caixa alta. <c>"EDP São Paulo S.A."</c> e <c>"EDP SAO PAULO S/A"</c> chegam
    /// os dois a <c>"EDPSAOPAULOSA"</c>.
    /// </summary>
    /// <remarks>
    /// O espaço cai junto com a pontuação de propósito: <c>"S.A."</c>, <c>"S/A"</c> e
    /// <c>"S A"</c> descrevem a mesma coisa, e preservar o espaço faria as três divergirem.
    /// </remarks>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
    }

    /// <summary>Os dois nomes descrevem o mesmo, ignorando grafia? Vazio nunca casa com nada.</summary>
    public static bool AreEquivalent(string? left, string? right)
    {
        var a = Normalize(left);
        return a.Length > 0 && string.Equals(a, Normalize(right), StringComparison.Ordinal);
    }
}
