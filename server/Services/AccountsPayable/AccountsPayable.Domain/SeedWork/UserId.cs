namespace AccountsPayable.Domain.SeedWork;

/// <summary>
/// Strongly-typed identifier for the actor performing a domain action. The User aggregate
/// itself lives in another Bounded Context (Identity / Access Control) — this type only
/// exists so aggregates here can record "who did what" without leaking a raw <see cref="Guid"/>.
/// </summary>
public readonly record struct UserId(Guid Value) : IEntityId<UserId>
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId From(Guid value) => new(value);
    public static UserId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
