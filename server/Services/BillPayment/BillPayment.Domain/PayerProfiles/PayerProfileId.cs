namespace BillPayment.Domain.PayerProfiles;

using BillPayment.Domain.SeedWork;

public readonly record struct PayerProfileId(Guid Value) : IEntityId<PayerProfileId>
{
    public static PayerProfileId New() => new(Guid.CreateVersion7());
    public static PayerProfileId From(Guid value) => new(value);
    public static PayerProfileId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
