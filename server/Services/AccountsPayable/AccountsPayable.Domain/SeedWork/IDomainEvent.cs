namespace AccountsPayable.Domain.SeedWork;

/// <summary>
/// Common shape for every Domain Event. Concrete events are <c>sealed record</c>s implementing this contract,
/// emitted from Aggregate Roots (traditional) via <c>AddDomainEvent</c> or recorded via <c>Apply</c> (Event-Sourced).
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
