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
/// <strong>A competência decide primeiro; a janela de dias é reserva.</strong> A variação real do
/// vencimento acontece <em>dentro do mês</em> — dia 8, 10 ou 12 de setembro descrevem a mesma
/// conta de setembro —, e comparar o mês resolve esse caso com exatidão, sem janela nenhuma. A
/// tolerância em dias sobra para o único caso que a competência não alcança: o vencimento que
/// atravessa a virada do mês (vence 30/09, é emitido como 01/10). Por isso ela encolheu de quinze
/// dias para <see cref="DUE_DATE_TOLERANCE_DAYS"/>: quinze dias existiam para compensar um ciclo
/// que abria tarde demais, e o ciclo deixou de abrir tarde.
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
    /// Tolerância entre o vencimento do boleto e o esperado do ciclo, quando a competência não
    /// resolve.
    /// </summary>
    /// <remarks>
    /// Três dias cobrem o vencimento empurrado para o dia útil seguinte na virada do mês, que é o
    /// único caso que sobra depois de a competência decidir. Medido no arquivo real: a variação
    /// de vencimento fica em três dias na quase totalidade das contas.
    /// </remarks>
    public const int DUE_DATE_TOLERANCE_DAYS = 3;

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

        var watching = candidates.Where(e => e.IsWatchingOn(today)).ToList();
        var competence = new CompetencePeriod(dueDate.Year, dueDate.Month);

        // 1º) a competência do vencimento. Exato, e imune à variação de dias dentro do mês.
        var byCompetence = Single(watching, c => c.Competence.Equals(competence));
        if (byCompetence is not null)
            return byCompetence;

        // 2º) a virada do mês, e só ela.
        return Single(
            watching,
            c => Math.Abs(c.ExpectedDueDate.DayNumber - dueDate.DayNumber) <= DUE_DATE_TOLERANCE_DAYS);
    }

    /// <summary>
    /// A única expectativa vigiando que ainda não tem ciclo para aquela competência.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É a rede de segurança contra prazo de chegada subestimado.</strong> O ciclo abre
    /// <c>ObservedLeadDays</c> antes do vencimento; uma conta que chegue antes disso — porque o
    /// emissor antecipou, ou porque o prazo aprendido ainda é curto — não encontraria ciclo nenhum
    /// e viraria alerta de "não chegou" sobre um boleto capturado. Aqui ela abre o próprio ciclo.
    /// </para>
    /// <para>
    /// <strong>Exige unicidade, como todo o resto deste serviço.</strong> Havendo duas contas do
    /// mesmo beneficiário, abrir o ciclo em qualquer uma delas seria escolher — e escolher errado
    /// apaga o alerta da conta que de fato não chegou.
    /// </para>
    /// </remarks>
    public static BillExpectationId? SoleWatchingWithoutCycleFor(
        IReadOnlyCollection<BillExpectation> candidates,
        CompetencePeriod competence,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(competence);

        var eligible = candidates
            .Where(e => e.IsWatchingOn(today) && e.CycleFor(competence) is null)
            .Take(2)
            .ToList();

        return eligible.Count == 1 ? eligible[0].Id : null;
    }

    /// <summary>
    /// Exatamente um, ou nada. O <c>Take(2)</c> basta porque a partir do segundo o desfecho já é
    /// "ambíguo" — ver a nota sobre ambiguidade no resumo do serviço.
    /// </summary>
    private static ExpectationMatch? Single(
        IReadOnlyCollection<BillExpectation> candidates,
        Func<ExpectationCycle, bool> predicate)
    {
        var matches = candidates
            .SelectMany(
                e => e.Cycles.Where(c => c.Status.IsOpen && predicate(c)),
                (e, c) => ExpectationMatch.Of(e.Id, c.Id))
            .Take(2)
            .ToList();

        return matches.Count == 1 ? matches[0] : null;
    }
}
