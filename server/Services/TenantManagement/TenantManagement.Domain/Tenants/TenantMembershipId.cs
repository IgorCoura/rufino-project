namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;

public readonly record struct TenantMembershipId(Guid Value) : IEntityId<TenantMembershipId>
{
    public static TenantMembershipId New() => new(Guid.CreateVersion7());
    public static TenantMembershipId From(Guid value) => new(value);
    public static TenantMembershipId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
