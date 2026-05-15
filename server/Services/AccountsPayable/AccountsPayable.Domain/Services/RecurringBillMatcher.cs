namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.ExpectedRecurringBills;
using AccountsPayable.Domain.ExpectedRecurringBills.Enumerations;
using AccountsPayable.Domain.Payables;

/// <summary>
/// Stateless Domain Service that picks the best Pending <see cref="ExpectedRecurringBill"/> for
/// an incoming <see cref="Payable"/>. Match criteria:
/// <list type="number">
///   <item>Same tenant.</item>
///   <item>Same <c>SupplierId</c>.</item>
///   <item>Same year-month of due date (calendar-month proximity).</item>
///   <item>Amount within ±<see cref="AmountTolerance"/> of the expected amount (default ±5%).</item>
/// </list>
/// When multiple bills qualify, the closest by absolute amount difference wins.
/// </summary>
public sealed class RecurringBillMatcher
{
    public const decimal DEFAULT_AMOUNT_TOLERANCE = 0.05m; // ±5%

    public decimal AmountTolerance { get; }

    public RecurringBillMatcher(decimal amountTolerance = DEFAULT_AMOUNT_TOLERANCE)
    {
        if (amountTolerance < 0m || amountTolerance > 1m)
            throw new ArgumentOutOfRangeException(nameof(amountTolerance), "Tolerância deve estar em [0, 1].");
        AmountTolerance = amountTolerance;
    }

    public ExpectedRecurringBill? FindMatch(Payable payable, IReadOnlyList<ExpectedRecurringBill> candidates)
    {
        ArgumentNullException.ThrowIfNull(payable);
        ArgumentNullException.ThrowIfNull(candidates);

        ExpectedRecurringBill? best = null;
        decimal bestDelta = decimal.MaxValue;

        foreach (var bill in candidates)
        {
            if (bill.Status != ExpectedRecurringBillStatus.Pending) continue;
            if (bill.TenantId != payable.TenantId) continue;
            if (bill.SupplierId != payable.SupplierId) continue;
            if (!SameYearMonth(bill.ExpectedDueDate, payable.DueDate.Value)) continue;

            var delta = Math.Abs(bill.ExpectedAmount.Amount - payable.Amount.Amount);
            var maxDelta = bill.ExpectedAmount.Amount * AmountTolerance;
            if (delta > maxDelta) continue;

            if (delta < bestDelta)
            {
                best = bill;
                bestDelta = delta;
            }
        }

        return best;
    }

    private static bool SameYearMonth(DateOnly a, DateOnly b) => a.Year == b.Year && a.Month == b.Month;
}
