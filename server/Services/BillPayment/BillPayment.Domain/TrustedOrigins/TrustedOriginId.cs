namespace BillPayment.Domain.TrustedOrigins;

using BillPayment.Domain.SeedWork;

public readonly record struct TrustedOriginId(Guid Value) : IEntityId<TrustedOriginId>
{
    public static TrustedOriginId New() => new(Guid.CreateVersion7());
    public static TrustedOriginId From(Guid value) => new(value);
    public static TrustedOriginId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
