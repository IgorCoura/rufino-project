namespace AccountsPayable.UnitTests.SeedWork._TestDoubles;

using AccountsPayable.Domain.SeedWork;

internal sealed record TestCreated(Guid EventId, DateTime OccurredAt, TestId Id, string Name) : IDomainEvent;
internal sealed record TestRenamed(Guid EventId, DateTime OccurredAt, string NewName) : IDomainEvent;
internal sealed record TestUnhandled(Guid EventId, DateTime OccurredAt) : IDomainEvent;

internal sealed class TestEventSourcedAggregate : EventSourcedAggregateRoot<TestId>
{
    public string? Name { get; private set; }

    private TestEventSourcedAggregate() : base() { }
    private TestEventSourcedAggregate(IEnumerable<IDomainEvent> history) : base(history) { }

    public static TestEventSourcedAggregate Initialize(TestId id, string name, DateTime occurredAt)
    {
        var instance = new TestEventSourcedAggregate();
        instance.Apply(new TestCreated(Guid.NewGuid(), occurredAt, id, name));
        return instance;
    }

    public static TestEventSourcedAggregate Rehydrate(IEnumerable<IDomainEvent> history)
        => new(history);

    public void Rename(string newName, DateTime occurredAt)
        => Apply(new TestRenamed(Guid.NewGuid(), occurredAt, newName));

    public void ApplyUnhandled(DateTime occurredAt)
        => Apply(new TestUnhandled(Guid.NewGuid(), occurredAt));

    private void When(TestCreated e)
    {
        Id = e.Id;
        Name = e.Name;
    }

    private void When(TestRenamed e)
    {
        Name = e.NewName;
    }
}
