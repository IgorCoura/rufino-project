namespace AccountsPayable.Domain.SeedWork;

/// <summary>
/// Contract for strongly-typed Ids in the AccountsPayable domain.
/// Each Aggregate/Entity defines its own <c>readonly record struct</c>
/// (PayableId, SupplierId, TenantId, etc.) implementing this interface.
/// <para>
/// Requires C# 11+ (static abstract interface members) — guaranteed by .NET 8 / C# 12.
/// </para>
/// </summary>
public interface IEntityId<TSelf> where TSelf : struct, IEntityId<TSelf>
{
    Guid Value { get; }
    static abstract TSelf New();
    static abstract TSelf From(Guid value);
    static abstract TSelf Empty { get; }
}
