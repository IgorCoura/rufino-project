namespace AccountsPayable.Domain.SeedWork;

/// <summary>
/// Strongly-typed tenant discriminator. The Tenant aggregate itself lives in another
/// Bounded Context — this type only exists so every aggregate in <c>AccountsPayable</c>
/// can filter and authorize by tenant without leaking a raw <see cref="Guid"/>.
/// <para>
/// .NET 8 lacks <c>Guid.CreateVersion7()</c> (.NET 9+) — using <see cref="Guid.NewGuid"/>.
/// Replace by the v7 generator when the BC moves to .NET 9+ for time-ordered ids.
/// </para>
/// </summary>
public readonly record struct TenantId(Guid Value) : IEntityId<TenantId>
{
    public static TenantId New() => new(Guid.NewGuid());
    public static TenantId From(Guid value) => new(value);
    public static TenantId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
