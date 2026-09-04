namespace BillPayment.Infra.Extraction;

using System.Text;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;

/// <summary>
/// Varre texto solto atrás de instrumentos de pagamento — o degrau 2 da cascata, gratuito e
/// instantâneo.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Gerar e validar, não reconhecer.</strong> Não há regex que descreva uma linha
/// digitável com segurança: ela aparece com pontos, espaços, quebrada em várias posições, ou
/// colada em outro número. A estratégia é o contrário — produzir todas as janelas plausíveis e
/// deixar o <c>DigitableLine</c> reprovar o que não presta. Construir a instância <em>é</em> a
/// prova dos DVs.
/// </para>
/// <para>
/// <strong>DV não basta, e isso foi medido.</strong> Uma janela de 47 dígitos de lixo do corpus
/// real passou nos quatro dígitos verificadores (<c>banco=000</c>, valor de R$ 4.411.000,00). O
/// VO rejeita banco não atribuído justamente por causa dela — o filtro de plausibilidade vive
/// no domínio, não aqui.
/// </para>
/// <para>
/// Nada aqui loga o texto varrido: um boleto no meio dele é instrumento de pagamento, e quem o
/// tem, paga.
/// </para>
/// </remarks>
internal static class CandidateScanner
{
    /// <summary>Linha digitável de cobrança bancária.</summary>
    private const int BANK_SLIP_LENGTH = 47;

    /// <summary>Linha digitável de arrecadação (contas de consumo, tributos).</summary>
    private const int UTILITY_LENGTH = 48;

    /// <summary>
    /// Teto de janelas por documento. Um PDF hostil com megabytes de dígitos não pode virar um
    /// laço caro — e nenhum boleto real precisa de mais que isso.
    /// </summary>
    private const int MAX_WINDOWS = 5_000;

    /// <summary>BR Code real fica bem abaixo disto; acima é texto arrastado, não payload.</summary>
    private const int MAX_PIX_PAYLOAD_LENGTH = 1024;

    /// <summary>Todo BR Code começa com o payload format indicator do EMV.</summary>
    private const string EMV_PREFIX = "000201";

    /// <param name="seen">
    /// Chaves naturais já encontradas, compartilhadas com o leitor de QR. Num boleto híbrido os
    /// dois caminhos podem achar o mesmo instrumento, e ele deve entrar uma vez só.
    /// </param>
    public static IReadOnlyList<PaymentInstrument> Scan(string? text, DateTime today, HashSet<string>? seen = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var found = new List<PaymentInstrument>();
        seen ??= new HashSet<string>(StringComparer.Ordinal);

        foreach (var instrument in ScanPixPayloads(text, seen))
            found.Add(instrument);

        foreach (var instrument in ScanDigitableLines(text, today, seen))
            found.Add(instrument);

        return found;
    }

    /// <summary>
    /// Procura BR Code no texto. Vem antes do código de barras porque o Pix é o trilho
    /// preferencial (ADR-010) — mas a ordem aqui é só de leitura; quem decide o trilho é o
    /// <c>Bill</c>.
    /// </summary>
    private static IEnumerable<PaymentInstrument> ScanPixPayloads(string text, HashSet<string> seen)
    {
        var start = text.IndexOf(EMV_PREFIX, StringComparison.Ordinal);
        var windows = 0;

        while (start >= 0)
        {
            // O mesmo teto de janelas da linha digitável: sem ele, um texto hostil com o prefixo
            // repetido milhares de vezes fazia o worker copiar o corpo inteiro a cada ocorrência
            // (auditoria 2026-08-28 — explosão quadrática por e-mail construído de propósito).
            if (++windows > MAX_WINDOWS)
                yield break;

            // O payload termina no CRC: "6304" + 4 hexadecimais. Procurar o fim assim evita
            // arrastar o resto da página para dentro do candidato — e o teto de tamanho impede
            // que um "6304" distante arraste um trecho gigante: BR Code real cabe em 512.
            var crc = text.IndexOf("6304", start + EMV_PREFIX.Length, StringComparison.Ordinal);

            if (crc >= 0 && crc + 8 <= text.Length && crc + 8 - start <= MAX_PIX_PAYLOAD_LENGTH)
            {
                var candidate = text[start..(crc + 8)];

                // Parse valida o CRC-16 sobre o payload inteiro: QR lido pela metade ou com
                // ruído de digitalização morre aqui, não vira pagamento.
                if (TryBuild(() => PaymentInstrument.FromPixQr(PixPayload.Parse(candidate)), seen, out var instrument))
                    yield return instrument!;
            }

            start = text.IndexOf(EMV_PREFIX, start + 1, StringComparison.Ordinal);
        }
    }

    private static IEnumerable<PaymentInstrument> ScanDigitableLines(
        string text,
        DateTime today,
        HashSet<string> seen)
    {
        var windows = 0;

        foreach (var run in DigitRuns(text))
        {
            foreach (var length in (int[])[BANK_SLIP_LENGTH, UTILITY_LENGTH])
            {
                for (var offset = 0; offset + length <= run.Length; offset++)
                {
                    if (++windows > MAX_WINDOWS)
                        yield break;

                    var candidate = run.Substring(offset, length);

                    if (TryBuild(() => PaymentInstrument.FromBarcode(DigitableLine.Parse(candidate, today)), seen, out var instrument))
                        yield return instrument!;
                }
            }
        }
    }

    /// <summary>
    /// Sequências de dígitos, ignorando a formatação que os emissores usam para deixar a linha
    /// legível — ponto, espaço, hífen.
    /// </summary>
    /// <remarks>
    /// Quebra de linha <strong>encerra</strong> a sequência: emenda entre linhas diferentes
    /// produziria números que não existem no documento, e um deles poderia passar nos DVs por
    /// acaso — que é exatamente o falso positivo já observado no corpus.
    /// </remarks>
    private static IEnumerable<string> DigitRuns(string text)
    {
        var buffer = new StringBuilder();

        foreach (var character in text)
        {
            if (char.IsAsciiDigit(character))
            {
                buffer.Append(character);
                continue;
            }

            var isFormatting = character is '.' or ' ' or '-' or '\t';

            if (isFormatting)
                continue;

            if (buffer.Length >= BANK_SLIP_LENGTH)
                yield return buffer.ToString();

            buffer.Clear();
        }

        if (buffer.Length >= BANK_SLIP_LENGTH)
            yield return buffer.ToString();
    }

    /// <summary>
    /// Constrói o instrumento e ignora o que o domínio reprovar.
    /// </summary>
    /// <remarks>
    /// <c>DomainException</c> aqui é <strong>fluxo normal</strong>, não falha: a varredura tenta
    /// milhares de janelas sabendo que quase todas são lixo. É o único lugar do BC onde engolir
    /// essa exceção é correto — em qualquer outro, ela significa invariante violada.
    /// </remarks>
    private static bool TryBuild(
        Func<PaymentInstrument> build,
        HashSet<string> seen,
        out PaymentInstrument? instrument)
    {
        instrument = null;

        try
        {
            var candidate = build();

            // A chave natural deduplica o mesmo instrumento impresso duas vezes na página —
            // comum em boleto com canhoto.
            if (!seen.Add(candidate.NaturalKey))
                return false;

            instrument = candidate;
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }
}
