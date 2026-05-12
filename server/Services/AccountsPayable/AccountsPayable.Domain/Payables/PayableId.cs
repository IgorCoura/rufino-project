namespace AccountsPayable.Domain.Payables;

using AccountsPayable.Domain.SeedWork;

public readonly record struct PayableId(Guid Value) : IEntityId<PayableId>
{
    public static PayableId New() => new(Guid.NewGuid());
    public static PayableId From(Guid value) => new(value);
    public static PayableId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
