namespace AccountsPayable.Domain.ExpectedRecurringBills;

using AccountsPayable.Domain.SeedWork;

public readonly record struct ExpectedRecurringBillId(Guid Value) : IEntityId<ExpectedRecurringBillId>
{
    public static ExpectedRecurringBillId New() => new(Guid.NewGuid());
    public static ExpectedRecurringBillId From(Guid value) => new(value);
    public static ExpectedRecurringBillId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
