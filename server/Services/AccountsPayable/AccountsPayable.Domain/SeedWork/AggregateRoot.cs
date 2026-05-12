namespace AccountsPayable.Domain.SeedWork;

/// <summary>
/// Base for traditional (snapshot-persisted) Aggregate Roots. Carries the Domain Event buffer
/// drained by the repository / Unit of Work after persistence.
/// <para>
/// For Event-Sourced aggregates (D-405: <c>Payable</c>) use
/// <see cref="EventSourcedAggregateRoot{TId}"/> instead.
/// </para>
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : struct, IEntityId<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() : base() { }

    protected AggregateRoot(TId id) : base(id) { }

    protected void AddDomainEvent(IDomainEvent eventItem) => _domainEvents.Add(eventItem);

    protected void RemoveDomainEvent(IDomainEvent eventItem) => _domainEvents.Remove(eventItem);

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Drains accumulated events. Repository / Unit of Work calls this after persisting
    /// the Aggregate, to forward events to the Outbox.
    /// </summary>
    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}
