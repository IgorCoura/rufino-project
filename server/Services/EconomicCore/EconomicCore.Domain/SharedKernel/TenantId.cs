namespace EconomicCore.Domain.SharedKernel;

using EconomicCore.Domain.SeedWork;

public readonly record struct TenantId(Guid Value) : IEntityId<TenantId>
{
    public static TenantId New() => new(Guid.CreateVersion7());
    public static TenantId From(Guid value) => new(value);
    public static TenantId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
