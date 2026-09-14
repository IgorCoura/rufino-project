namespace BillPayment.Domain.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;

/// <summary>Onde o número da conta foi encontrado no artefato.</summary>
public sealed class AccountReferenceEvidence : Enumeration
{
    /// <summary>Não aparece em lugar nenhum.</summary>
    public static readonly AccountReferenceEvidence None = new(0, nameof(None));

    /// <summary>Aparece como número isolado no texto do documento ou do e-mail.</summary>
    public static readonly AccountReferenceEvidence Text = new(1, nameof(Text));

    /// <summary>Aparece dentro do campo livre do código de barras, que é protegido por DV.</summary>
    public static readonly AccountReferenceEvidence Barcode = new(2, nameof(Barcode));

    private AccountReferenceEvidence(int id, string name) : base(id, name) { }
}

/// <summary>A expectativa cujo número de conta foi encontrado no artefato, e com que força.</summary>
public sealed class AccountReferenceMatch : ValueObject
{
    public BillExpectationId ExpectationId { get; }

    public RoutingConfidence Confidence { get; }

    private AccountReferenceMatch(BillExpectationId expectationId, RoutingConfidence confidence)
    {
        ExpectationId = expectationId;
        Confidence = confidence;
    }

    internal static AccountReferenceMatch Of(BillExpectationId expectationId, RoutingConfidence confidence)
        => new(expectationId, confidence);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ExpectationId;
        yield return Confidence;
    }
}

/// <summary>
/// Procura, num boleto capturado, o número da conta que o tenant cadastrou numa expectativa — o
/// degrau 3 da escada de roteamento (ADR-026).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque conta de concessionária quase nunca imprime o documento do
/// pagador</strong>, mas sempre imprime a conta do cliente: Nº da conta na Vivo, instalação na EDP,
/// matrícula no DAE. Na arrecadação esse número costuma estar dentro do próprio código de barras.
/// </para>
/// <para>
/// <strong>Não contradiz a medição da sprint 2.6</strong>, que derrubou a <c>RoutingRule</c>: lá a
/// referência era DEDUZIDA de posição fixa do campo livre, e em boleto de cobrança a parte estável é
/// a conta do beneficiário. Aqui o número é INFORMADO pelo tenant e procurado por conter, nunca por
/// posição.
/// </para>
/// <para>
/// É Domain Service porque cruza dois Aggregates — o artefato capturado e as expectativas do
/// tenant. Estático e puro, como os outros do BC.
/// </para>
/// </remarks>
public static class AccountReferenceMatchingService
{
    /// <summary>Abaixo disto a chance de coincidência é alta demais para rotear.</summary>
    public const int MIN_SIGNIFICANT_DIGITS = 6;

    /// <summary>A partir disto o número dentro do código de barras vale como prova forte.</summary>
    public const int STRONG_SIGNIFICANT_DIGITS = 8;

    /// <summary>O campo livre começa na posição 20 do código de barras, nos dois tipos de boleto.</summary>
    private const int FREE_FIELD_START = 19;

    /// <summary>
    /// Os dígitos que identificam a conta: só dígitos, sem zeros à esquerda. Nulo quando sobram
    /// menos que <see cref="MIN_SIGNIFICANT_DIGITS"/> — número curto não roteia.
    /// </summary>
    /// <remarks>
    /// Os zeros à esquerda saem dos dois lados porque o emissor completa o campo à vontade: a Vivo
    /// imprime <c>00001123004411</c> para a conta <c>1123004411</c>. Letras saem junto — a
    /// matrícula <c>L18502</c> do DAE vira <c>18502</c>, que é curta e não roteia (limite aceito
    /// pelo usuário em 2026-09-14).
    /// </remarks>
    public static string? SignificantDigits(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        var digits = new string(reference.Where(char.IsAsciiDigit).ToArray()).TrimStart('0');

        return digits.Length >= MIN_SIGNIFICANT_DIGITS ? digits : null;
    }

    /// <summary>A expectativa deste tenant cujo número de conta está no artefato, se houver uma só.</summary>
    /// <param name="texts">
    /// O texto do documento e o do corpo do e-mail. Aceita HTML cru: a marcação quebra as sequências
    /// de dígitos exatamente onde o texto as quebraria.
    /// </param>
    /// <returns>
    /// Nulo quando nenhuma casa — <strong>e também quando mais de uma casa</strong>: escolher entre
    /// duas contas do mesmo tenant seria adivinhar, e a fila de reivindicação é o lugar da dúvida.
    /// </returns>
    public static AccountReferenceMatch? Match(
        IReadOnlyCollection<PaymentInstrument> instruments,
        IReadOnlyCollection<string?> texts,
        IEnumerable<BillExpectation> expectations)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentNullException.ThrowIfNull(expectations);

        var matches = new List<AccountReferenceMatch>();

        foreach (var expectation in expectations)
        {
            var digits = SignificantDigits(expectation.AccountReference);
            if (digits is null)
                continue;

            var evidence = Locate(digits, instruments, texts);
            if (evidence == AccountReferenceEvidence.None)
                continue;

            var confidence = evidence == AccountReferenceEvidence.Barcode && digits.Length >= STRONG_SIGNIFICANT_DIGITS
                ? RoutingConfidence.Strong
                : RoutingConfidence.Weak;

            matches.Add(AccountReferenceMatch.Of(expectation.Id, confidence));
        }

        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>Onde os dígitos significativos de uma conta aparecem no artefato.</summary>
    /// <param name="significantDigits">A saída de <see cref="SignificantDigits"/>.</param>
    public static AccountReferenceEvidence Locate(
        string significantDigits,
        IReadOnlyCollection<PaymentInstrument> instruments,
        IReadOnlyCollection<string?> texts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(significantDigits);
        ArgumentNullException.ThrowIfNull(instruments);
        ArgumentNullException.ThrowIfNull(texts);

        var inBarcode = instruments
            .Where(i => i.Kind == PaymentInstrumentKind.Barcode)
            .Select(i => i.DigitableLine.Barcode)
            .Any(barcode => barcode.Length > FREE_FIELD_START
                && barcode[FREE_FIELD_START..].Contains(significantDigits, StringComparison.Ordinal));

        if (inBarcode)
            return AccountReferenceEvidence.Barcode;

        return texts.Any(text => AppearsIsolated(text, significantDigits))
            ? AccountReferenceEvidence.Text
            : AccountReferenceEvidence.None;
    }

    /// <summary>
    /// A conta é uma sequência de dígitos inteira do texto — ignorados pontos, traços, barras e
    /// espaços, e os zeros à esquerda.
    /// </summary>
    /// <remarks>
    /// <strong>Igualdade, nunca "contém".</strong> Dentro de um número maior do texto (um protocolo,
    /// um código de barras impresso) o acerto por acaso é plausível; como sequência inteira, não.
    /// </remarks>
    private static bool AppearsIsolated(string? text, string significantDigits)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        var run = new System.Text.StringBuilder();

        for (var i = 0; i <= text.Length; i++)
        {
            var character = i < text.Length ? text[i] : '\n';

            if (char.IsAsciiDigit(character))
            {
                run.Append(character);
                continue;
            }

            if (run.Length > 0 && character is '.' or '-' or '/' or ' ')
                continue;

            if (run.Length > 0
                && run.ToString().TrimStart('0').Equals(significantDigits, StringComparison.Ordinal))
            {
                return true;
            }

            run.Clear();
        }

        return false;
    }
}
