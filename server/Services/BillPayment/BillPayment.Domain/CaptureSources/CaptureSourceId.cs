namespace BillPayment.Domain.CaptureSources;

using BillPayment.Domain.SeedWork;

public readonly record struct CaptureSourceId(Guid Value) : IEntityId<CaptureSourceId>
{
    public static CaptureSourceId New() => new(Guid.CreateVersion7());
    public static CaptureSourceId From(Guid value) => new(value);
    public static CaptureSourceId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
