namespace BillPayment.Domain.Payees;

using BillPayment.Domain.SeedWork;

public readonly record struct PayeeId(Guid Value) : IEntityId<PayeeId>
{
    public static PayeeId New() => new(Guid.CreateVersion7());
    public static PayeeId From(Guid value) => new(value);
    public static PayeeId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
