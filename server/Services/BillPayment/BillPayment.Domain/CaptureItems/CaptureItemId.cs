namespace BillPayment.Domain.CaptureItems;

using BillPayment.Domain.SeedWork;

public readonly record struct CaptureItemId(Guid Value) : IEntityId<CaptureItemId>
{
    public static CaptureItemId New() => new(Guid.CreateVersion7());
    public static CaptureItemId From(Guid value) => new(value);
    public static CaptureItemId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
