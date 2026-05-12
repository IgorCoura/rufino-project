namespace AccountsPayable.Domain.Suppliers;

using AccountsPayable.Domain.SeedWork;

public readonly record struct SupplierId(Guid Value) : IEntityId<SupplierId>
{
    public static SupplierId New() => new(Guid.NewGuid());
    public static SupplierId From(Guid value) => new(value);
    public static SupplierId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
