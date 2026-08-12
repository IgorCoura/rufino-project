namespace BillPayment.UnitTests.Expectations.Mothers;

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
        DateTime? occurredAt = null)
        => BillExpectation.Register(
            DefaultTenant,
            DefaultPayee,
            accountReference,
            DefaultLabel,
            recurrence ?? Recurrence.Monthly,
            expectedDueDay,
            observedLeadDays,
            alertLeadDays,
            occurredAt ?? DefaultOccurredAt);

    public static BillExpectation Learned(int observationCount = 3, int expectedDueDay = 10)
        => BillExpectation.Learn(
            DefaultTenant,
            DefaultPayee,
            DefaultLabel,
            Recurrence.Monthly,
            expectedDueDay,
            observedLeadDays: 8,
            observationCount,
            hintSourceId: null,
            DefaultOccurredAt);

    /// <summary>Com o ciclo da competência informada já aberto.</summary>
    public static (BillExpectation Expectation, ExpectationCycle Cycle) WithOpenCycle(
        int year = 2026, int month = 8, BillExpectation? expectation = null)
    {
        var target = expectation ?? Register();
        var cycle = target.OpenCycle(new CompetencePeriod(year, month), DefaultOccurredAt);

        return (target, cycle);
    }
}
