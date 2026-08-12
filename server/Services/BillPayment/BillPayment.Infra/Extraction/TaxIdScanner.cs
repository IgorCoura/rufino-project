namespace BillPayment.Infra.Extraction;

using System.Text.RegularExpressions;
using BillPayment.Domain.Extraction;

/// <summary>
/// Varre texto solto atrás dos documentos fiscais impressos no artefato — o insumo do degrau 1
/// da escada de roteamento.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Sequência exata, nunca janela deslizante</strong> — e esta é a diferença entre a
/// varredura de documento fiscal e a de linha digitável, que faz o oposto. O CNPJ tem só dois
/// dígitos verificadores, então uma janela de 14 dígitos tem cerca de 1% de chance de passar por
/// acaso; um código de barras de 44 posições oferece trinta e uma janelas. Medido em 2026-08-12
/// sobre 714 documentos: a regra deslizante <strong>fabricaria um CNPJ aparentemente válido
/// dentro do código de barras em 46,9% deles</strong>. Como um documento fabricado pode cair ao
/// lado de um rótulo de pagador, ele mandaria para a quarentena cega uma conta legítima.
/// </para>
/// <para>
/// Exigir que a sequência inteira tenha 11 ou 14 dígitos elimina isso por construção — um bloco
/// de 44 não é nenhum dos dois — sem custo de cobertura: o documento do tenant continua sendo
/// encontrado em 93,3% dos boletos do corpus, porque emissor imprime documento fiscal isolado ou
/// formatado, nunca colado a outro número.
/// </para>
/// <para>
/// <strong>O DV continua sendo quem decide.</strong> A varredura propõe a sequência e
/// <c>PartyCandidate.TryCreate</c> reprova o que não fecha (ADR-011).
/// </para>
/// <para>
/// Nada aqui é logado: documento fiscal de pagador é dado pessoal.
/// </para>
/// </remarks>
internal static partial class TaxIdScanner
{
    private const int CPF_LENGTH = 11;
    private const int CNPJ_LENGTH = 14;

    /// <summary>
    /// Quanto texto antes da ocorrência é examinado atrás do rótulo. 260 caracteres é o que
    /// cobre o bloco "Pagador / nome / endereço / CNPJ" dos layouts medidos sem alcançar o bloco
    /// anterior do documento.
    /// </summary>
    private const int LABEL_LOOKBACK = 260;

    /// <summary>
    /// Teto de documentos fiscais por artefato. Uma folha de rosto de contabilidade lista
    /// dezenas; passar disso não melhora o roteamento e só alimenta laço.
    /// </summary>
    private const int MAX_PARTIES = 40;

    /// <summary>
    /// Os cinco rótulos observados no corpus, por frequência: pagador (103), tomador (92),
    /// sacado (4), cliente (4) — mais contribuinte, que é o de guia de tributo.
    /// </summary>
    [GeneratedRegex(@"pagador|sacado|tomador|cliente|contribuinte|devedor",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PayerLabel();

    /// <summary>
    /// O outro lado. Existe para desempatar: num boleto os dois blocos são vizinhos, e sem isto
    /// o rótulo do credor seria confundido com o do devedor por simples proximidade.
    /// </summary>
    [GeneratedRegex(@"benefici[áa]rio|cedente|favorecido|credor|sacador",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PayeeLabel();

    public static IReadOnlyList<PartyCandidate> Scan(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var found = new List<PartyCandidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (digits, start) in DigitRuns(text))
        {
            if (found.Count >= MAX_PARTIES)
                break;

            var candidate = PartyCandidate.TryCreate(digits, IsUnderPayerLabel(text, start));

            // Deduplica por documento E por rótulo: o mesmo CNPJ costuma aparecer no bloco do
            // pagador e no rodapé sem rótulo nenhum, e é a ocorrência rotulada que importa.
            if (candidate is not null && seen.Add($"{candidate.TaxId.Value}|{candidate.UnderPayerLabel}"))
                found.Add(candidate);
        }

        return found;
    }

    /// <summary>
    /// Sequências de dígitos de tamanho exato, com a posição onde começam no texto original.
    /// </summary>
    /// <remarks>
    /// A barra entra na formatação ignorada — sem ela <c>02.624.917/0001-92</c> viraria duas
    /// sequências curtas e nenhum CNPJ seria lido. Quebra de linha encerra a sequência, pelo
    /// mesmo motivo do <c>CandidateScanner</c>: emendar linhas diferentes produz números que não
    /// existem no documento.
    /// </remarks>
    private static IEnumerable<(string Digits, int Start)> DigitRuns(string text)
    {
        var length = 0;
        var start = -1;

        for (var i = 0; i <= text.Length; i++)
        {
            var character = i < text.Length ? text[i] : '\n';

            if (char.IsAsciiDigit(character))
            {
                if (length == 0)
                    start = i;

                length++;
                continue;
            }

            if (character is '.' or '-' or '/' or ' ' or '\t')
                continue;

            if (length is CPF_LENGTH or CNPJ_LENGTH)
                yield return (Digits(text, start, i), start);

            length = 0;
        }
    }

    private static string Digits(string text, int start, int end)
        => string.Concat(text[start..end].Where(char.IsAsciiDigit));

    /// <summary>
    /// O rótulo mais próximo antes da ocorrência é de pagador, e não de beneficiário.
    /// </summary>
    /// <remarks>
    /// <strong>Só serve para negar, nunca para afirmar.</strong> A atribuição do boleto vem de
    /// casar com o cadastro do próprio tenant, que é seguro sem rótulo nenhum; o rótulo é o que
    /// autoriza a afirmação contrária — "este é de outra pessoa" —, e só 66,8% das ocorrências o
    /// têm. Ver <c>BillRoutingService</c>.
    /// </remarks>
    private static bool IsUnderPayerLabel(string text, int start)
    {
        var window = text[Math.Max(0, start - LABEL_LOOKBACK)..start];

        var payer = LastIndexOf(PayerLabel(), window);
        return payer >= 0 && payer > LastIndexOf(PayeeLabel(), window);
    }

    private static int LastIndexOf(Regex pattern, string window)
    {
        var index = -1;

        foreach (var match in pattern.Matches(window).Cast<Match>())
            index = match.Index;

        return index;
    }
}
