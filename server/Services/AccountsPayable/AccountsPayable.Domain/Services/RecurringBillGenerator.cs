namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.Contracts;
using AccountsPayable.Domain.ExpectedRecurringBills;

/// <summary>
/// Stateless Domain Service that materializes <see cref="ExpectedRecurringBill"/> aggregates from
/// an active <see cref="Contract"/>. Cross-Aggregate orchestration — the Contract doesn't reach
/// across boundaries to create ERBs itself.
/// <para>
/// <b>Due-date clamping</b>: <c>PaymentDay</c> can be up to 31. When the target month has fewer
/// days (Feb, Apr, Jun, Sep, Nov), the day is clamped to the last day of that month so the
/// returned <see cref="DateOnly"/> is always valid.
/// </para>
/// </summary>
public sealed class RecurringBillGenerator
{
    /// <summary>
    /// Generate one <see cref="ExpectedRecurringBill"/> per month in the inclusive range
    /// <c>[<paramref name="fromMonth"/>, <paramref name="fromMonth"/> + <paramref name="monthCount"/>)</c>.
    /// Useful for batch-creating the first 12 months of a yearly contract (Sprint 11 critério de aceite).
    /// </summary>
    public IReadOnlyList<ExpectedRecurringBill> Generate(
        Contract contract,
        DateOnly fromMonth,
        int monthCount,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(contract);
        if (monthCount < 1)
            return Array.Empty<ExpectedRecurringBill>();

        var bills = new List<ExpectedRecurringBill>(monthCount);
        for (var i = 0; i < monthCount; i++)
        {
            var target = fromMonth.AddMonths(i);
            var lastDayOfMonth = DateTime.DaysInMonth(target.Year, target.Month);
            var actualDay = Math.Min(contract.PaymentDay, lastDayOfMonth);
            var dueDate = new DateOnly(target.Year, target.Month, actualDay);

            bills.Add(ExpectedRecurringBill.ForContract(
                id: ExpectedRecurringBillId.New(),
                tenantId: contract.TenantId,
                contractId: contract.Id,
                supplierId: contract.SupplierId,
                expectedDueDate: dueDate,
                expectedAmount: contract.MonthlyAmount,
                occurredAt: occurredAt));
        }
        return bills.AsReadOnly();
    }
}
