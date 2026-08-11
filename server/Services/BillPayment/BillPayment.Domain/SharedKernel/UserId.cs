namespace BillPayment.Domain.SharedKernel;

using BillPayment.Domain.SeedWork;

public readonly record struct UserId(Guid Value) : IEntityId<UserId>
{
    public static UserId New() => new(Guid.CreateVersion7());
    public static UserId From(Guid value) => new(value);
    public static UserId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
