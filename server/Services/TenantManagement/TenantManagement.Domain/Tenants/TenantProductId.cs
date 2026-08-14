namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;

public readonly record struct TenantProductId(Guid Value) : IEntityId<TenantProductId>
{
    public static TenantProductId New() => new(Guid.CreateVersion7());
    public static TenantProductId From(Guid value) => new(value);
    public static TenantProductId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
