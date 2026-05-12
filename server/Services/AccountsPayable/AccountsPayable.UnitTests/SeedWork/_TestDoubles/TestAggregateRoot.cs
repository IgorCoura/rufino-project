namespace AccountsPayable.UnitTests.SeedWork._TestDoubles;

using AccountsPayable.Domain.SeedWork;

internal sealed class TestAggregateRoot : AggregateRoot<TestId>
{
    public TestAggregateRoot() : base() { }
    public TestAggregateRoot(TestId id) : base(id) { }

    public void RaiseEvent(IDomainEvent @event) => AddDomainEvent(@event);
    public void RetractEvent(IDomainEvent @event) => RemoveDomainEvent(@event);
}

internal sealed record TestEvent(Guid EventId, DateTime OccurredAt, string Label) : IDomainEvent;
