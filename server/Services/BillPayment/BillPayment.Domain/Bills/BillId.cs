namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

public readonly record struct BillId(Guid Value) : IEntityId<BillId>
{
    public static BillId New() => new(Guid.CreateVersion7());
    public static BillId From(Guid value) => new(value);
    public static BillId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
