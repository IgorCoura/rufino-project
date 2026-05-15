namespace AccountsPayable.Domain.ExpectedRecurringBills.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class ExpectedRecurringBillStatus : Enumeration
{
    public static readonly ExpectedRecurringBillStatus Pending = new(1, "PENDING");
    public static readonly ExpectedRecurringBillStatus MatchedToPayable = new(2, "MATCHED_TO_PAYABLE");
    public static readonly ExpectedRecurringBillStatus Missed = new(3, "MISSED");
    public static readonly ExpectedRecurringBillStatus Cancelled = new(4, "CANCELLED");

    private ExpectedRecurringBillStatus(int id, string name) : base(id, name) { }
}
