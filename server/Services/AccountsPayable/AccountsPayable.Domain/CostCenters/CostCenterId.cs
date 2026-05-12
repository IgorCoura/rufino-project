namespace AccountsPayable.Domain.CostCenters;

using AccountsPayable.Domain.SeedWork;

public readonly record struct CostCenterId(Guid Value) : IEntityId<CostCenterId>
{
    public static CostCenterId New() => new(Guid.NewGuid());
    public static CostCenterId From(Guid value) => new(value);
    public static CostCenterId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
