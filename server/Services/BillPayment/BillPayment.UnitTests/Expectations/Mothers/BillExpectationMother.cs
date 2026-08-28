namespace BillPayment.UnitTests.Expectations.Mothers;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SharedKernel;

internal static class BillExpectationMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    public static readonly PayeeId DefaultPayee = PayeeId.From(new Guid("0195a1f0-0000-7000-8000-0000000000e1"));
    public static readonly UserId DefaultUser = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000f1"));

    public const string DefaultLabel = "EDP — Casa Florentino";

    /// <summary>Conta que vence no dia 10 e costuma chegar oito dias antes.</summary>
    public static BillExpectation Register(
        string? accountReference = null,
        Recurrence? recurrence = null,
        int expectedDueDay = 10,
        int observedLeadDays = 8,
        int? alertLeadDays = null,
        DateTime? occurredAt = null,
        DateOnly? anchorDueDate = null,
        CaptureSourceId? hintSourceId = null)
        => BillExpectation.Register(
            DefaultTenant,
            DefaultPayee,
            accountReference,
            DefaultLabel,
            recurrence ?? Recurrence.Monthly,
            expectedDueDay,
            observedLeadDays,
            alertLeadDays,
            anchorDueDate,
            hintSourceId,
            occurredAt ?? DefaultOccurredAt);

    public static BillExpectation Learned(
        int observationCount = 3,
        int expectedDueDay = 10,
        Recurrence? recurrence = null,
        CompetencePeriod? anchorCompetence = null,
        CaptureSourceId? hintSourceId = null)
        => BillExpectation.Learn(
            DefaultTenant,
            DefaultPayee,
            DefaultLabel,
            recurrence ?? Recurrence.Monthly,
            expectedDueDay,
            observedLeadDays: 8,
            observationCount,
            anchorCompetence,
            hintSourceId,
            DefaultOccurredAt);

    /// <summary>
    /// Cumprimento com a data de chegada explícita — é ela, e não o instante da chamada, que
    /// alimenta a média móvel do prazo observado.
    /// </summary>
    public static void Fulfill(
        BillExpectation expectation,
        ExpectationCycleId cycleId,
        BillId billId,
        DateOnly actualDueDate,
        DateOnly? arrivedOn = null,
        DateTime? occurredAt = null)
        => expectation.Fulfill(
            cycleId,
            billId,
            actualDueDate,
            arrivedOn ?? DateOnly.FromDateTime(occurredAt ?? DefaultOccurredAt),
            arrivedThrough: null,
            occurredAt ?? DefaultOccurredAt);

    /// <summary>Com o ciclo da competência informada já aberto.</summary>
    public static (BillExpectation Expectation, ExpectationCycle Cycle) WithOpenCycle(
        int year = 2026, int month = 8, BillExpectation? expectation = null)
    {
        var target = expectation ?? Register();
        var cycle = target.OpenCycle(new CompetencePeriod(year, month), DefaultOccurredAt);

        return (target, cycle);
    }
}
