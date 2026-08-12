namespace BillPayment.Domain.Services;

using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>Qual ciclo de qual expectativa um boleto que chegou cumpre.</summary>
public sealed class ExpectationMatch : ValueObject
{
    public BillExpectationId ExpectationId { get; }
    public ExpectationCycleId CycleId { get; }

    private ExpectationMatch(BillExpectationId expectationId, ExpectationCycleId cycleId)
    {
        ExpectationId = expectationId;
        CycleId = cycleId;
    }

    internal static ExpectationMatch Of(BillExpectationId expectationId, ExpectationCycleId cycleId)
        => new(expectationId, cycleId);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ExpectationId;
        yield return CycleId;
    }
}

/// <summary>
/// Cruza um boleto recém-capturado com os ciclos abertos das expectativas do beneficiário.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É Domain Service porque cruza dois Aggregates</strong> — <c>Bill</c> e
/// <c>BillExpectation</c> — e devolve valor: quem muta é <c>expectation.Fulfill(...)</c>.
/// Estático e puro, como os outros do BC.
/// </para>
/// <para>
/// <strong>Casa pelo vencimento, não pela data de chegada.</strong> A conta de agosto pode chegar
/// em julho ou em setembro; o que a identifica é o compromisso que ela liquida. Casar por chegada
/// faria uma conta adiantada cumprir o ciclo errado e deixar o correto alertando sozinho.
/// </para>
/// <para>
/// <strong>Ambiguidade não é resolvida por desempate.</strong> Se duas expectativas do mesmo
/// beneficiário têm ciclo aberto na janela — o caso das quatro instalações da EDP —, o serviço
/// devolve <c>null</c> em vez de escolher: cumprir a expectativa errada apagaria o alerta da
/// conta que de fato não chegou, que é a falha silenciosa que tudo isto existe para impedir.
/// </para>
/// </remarks>
public static class ExpectationMatchingService
{
    /// <summary>
    /// Tolerância entre o vencimento do boleto e o esperado do ciclo.
    /// </summary>
    /// <remarks>
    /// Quinze dias cobre o vencimento empurrado para dia útil e a variação de calendário de
    /// faturamento, sem alcançar o ciclo vizinho de uma conta mensal.
    /// </remarks>
    public const int DUE_DATE_TOLERANCE_DAYS = 15;

    /// <param name="candidates">
    /// As expectativas do mesmo beneficiário. Quem as carrega é o handler, filtrando por tenant.
    /// </param>
    /// <param name="billDueDate">
    /// O vencimento do boleto. Nulo — arrecadação sem data legível — impede o casamento, e o
    /// ciclo segue aberto: é melhor alertar por uma conta que chegou do que dar por cumprida a
    /// que não chegou.
    /// </param>
    public static ExpectationMatch? Match(
        IReadOnlyCollection<BillExpectation> candidates,
        DateOnly? billDueDate,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (billDueDate is not { } dueDate)
            return null;

        var matches = candidates
            .Where(e => e.IsWatchingOn(today))
            .SelectMany(
                e => e.Cycles.Where(c =>
                    c.Status.IsOpen
                    && Math.Abs(c.ExpectedDueDate.DayNumber - dueDate.DayNumber) <= DUE_DATE_TOLERANCE_DAYS),
                (e, c) => ExpectationMatch.Of(e.Id, c.Id))
            .Take(2)
            .ToList();

        // Exatamente um, ou nenhum. Ver a nota sobre ambiguidade no resumo do serviço — o Take(2)
        // basta porque a partir do segundo o desfecho já é "ambíguo".
        return matches.Count == 1 ? matches[0] : null;
    }
}
