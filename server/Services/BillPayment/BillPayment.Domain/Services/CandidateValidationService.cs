namespace BillPayment.Domain.Services;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;

/// <summary>
/// Converte o que o extrator de visão <em>propôs</em> no que o domínio aceita — e descarta o resto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É aqui que o ADR-011 deixa de ser texto e vira código.</strong> Nada que o modelo
/// escreveu entra no sistema sem sobreviver ao dígito verificador da linha digitável ou ao CRC do
/// BR Code. Uma linha alucinada — dígito trocado, número inventado, valor de outra parte da
/// página — não constrói <c>DigitableLine</c>, e sem instrumento não há boleto.
/// </para>
/// <para>
/// <strong>Reprovar é o caminho normal, não a exceção.</strong> Assim como no
/// <c>CandidateScanner</c>, a <c>DomainException</c> das factories é fluxo esperado: o modelo
/// propõe vários candidatos e a maioria é lixo. Estes são os dois únicos lugares do BC onde
/// engolir <c>DomainException</c> é correto — em qualquer outro, é defeito escondido.
/// </para>
/// <para>
/// <strong>Passar por aqui não torna nada verdadeiro.</strong> O DV prova que o número é
/// bem-formado, não que o título existe, nem de quem é, nem por quanto — isso é a consulta
/// oficial, no passo seguinte. Foi medido no corpus real que uma janela de 47 dígitos de lixo
/// passou nos quatro DVs por acaso.
/// </para>
/// <para>
/// Estático e puro, como <c>PasswordDerivationService</c>: sem estado, sem I/O, sem relógio — a
/// data entra por parâmetro porque o fator de vencimento é ambíguo entre duas épocas.
/// </para>
/// </remarks>
public static class CandidateValidationService
{
    private const int BARCODE_LENGTH = 44;

    /// <summary>
    /// Filtra os candidatos, devolvendo só os instrumentos que se sustentam.
    /// </summary>
    /// <param name="seen">
    /// Chaves naturais já vistas neste artefato, compartilhado com os degraus anteriores da
    /// cascata. Sem isso, o mesmo boleto lido pelo texto e pela visão viraria dois instrumentos.
    /// </param>
    public static IReadOnlyList<PaymentInstrument> Validate(
        ExtractedDocument document,
        DateTime today,
        HashSet<string>? seen = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var accepted = new List<PaymentInstrument>();
        var keys = seen ?? new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in document.DigitableLineCandidates)
            Accept(FromDigits(candidate, today), accepted, keys);

        foreach (var candidate in document.PixPayloadCandidates)
            Accept(FromPix(candidate), accepted, keys);

        return accepted;
    }

    private static void Accept(PaymentInstrument? instrument, List<PaymentInstrument> accepted, HashSet<string> keys)
    {
        if (instrument is not null && keys.Add(instrument.NaturalKey))
            accepted.Add(instrument);
    }

    /// <summary>
    /// Linha digitável ou código de barras, conforme a quantidade de dígitos.
    /// </summary>
    /// <remarks>
    /// O modelo devolve o número como está impresso — com pontos, espaços e às vezes quebrado.
    /// Só os dígitos importam, e o comprimento resultante diz o que é: 44 é o código de barras
    /// (que <c>FromBarcode</c> reconstrói e revalida), 47 e 48 são a linha digitável de cobrança
    /// e de arrecadação.
    /// </remarks>
    private static PaymentInstrument? FromDigits(string candidate, DateTime today)
    {
        var digits = new string(candidate.Where(char.IsAsciiDigit).ToArray());

        try
        {
            var line = digits.Length == BARCODE_LENGTH
                ? DigitableLine.FromBarcode(digits, today)
                : DigitableLine.Parse(digits, today);

            return PaymentInstrument.FromBarcode(line);
        }
        catch (DomainException)
        {
            // Candidato reprovado no DV, no banco não atribuído ou no comprimento. É o desfecho
            // esperado da maioria — o modelo propõe, o domínio dispõe.
            return null;
        }
    }

    private static PaymentInstrument? FromPix(string candidate)
    {
        try
        {
            return PaymentInstrument.FromPixQr(PixPayload.Parse(candidate));
        }
        catch (DomainException)
        {
            return null;
        }
    }
}
