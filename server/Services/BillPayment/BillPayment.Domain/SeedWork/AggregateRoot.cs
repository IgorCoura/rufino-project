namespace BillPayment.Domain.SeedWork;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents where TId : struct, IEntityId<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() : base() { }
    protected AggregateRoot(TId id) : base(id) { }

    protected void AddDomainEvent(IDomainEvent eventItem) => _domainEvents.Add(eventItem);
    protected void RemoveDomainEvent(IDomainEvent eventItem) => _domainEvents.Remove(eventItem);
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Drains accumulated events. Called by the repository/UoW after persisting, to forward to the Outbox.
    /// </summary>
    /// <remarks>
    /// Reached through the non-generic <see cref="IHasDomainEvents"/>, never through a per-aggregate
    /// type list — see the interface for why that distinction cost a month of missing webhooks.
    /// </remarks>
    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}
