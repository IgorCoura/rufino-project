namespace AccountsPayable.UnitTests.SeedWork;

using AccountsPayable.UnitTests.SeedWork._TestDoubles;

public class AggregateRootTests
{
    // Aggregate Root recém-criado começa com o buffer de Domain Events vazio.
    [Fact]
    public void NewAggregateRoot_HasNoDomainEvents()
    {
        var root = new TestAggregateRoot();
        Assert.Empty(root.DomainEvents);
    }

    // AddDomainEvent empilha eventos no buffer preservando a ordem de adição.
    [Fact]
    public void AddDomainEvent_AccumulatesEvents()
    {
        var root = new TestAggregateRoot();
        var e1 = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "first");
        var e2 = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "second");

        root.RaiseEvent(e1);
        root.RaiseEvent(e2);

        Assert.Equal(new[] { e1, e2 }, root.DomainEvents);
    }

    // PullDomainEvents retorna snapshot dos eventos pendentes e zera o buffer (drenagem para a Outbox).
    [Fact]
    public void PullDomainEvents_DrainsAndReturnsSnapshot()
    {
        var root = new TestAggregateRoot();
        var e1 = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "first");
        root.RaiseEvent(e1);

        var pulled = root.PullDomainEvents();

        Assert.Equal(new[] { e1 }, pulled);
        Assert.Empty(root.DomainEvents);
    }

    // ClearDomainEvents esvazia o buffer sem retornar nada (descarte intencional dos eventos pendentes).
    [Fact]
    public void ClearDomainEvents_EmptiesBuffer()
    {
        var root = new TestAggregateRoot();
        root.RaiseEvent(new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "x"));
        root.ClearDomainEvents();

        Assert.Empty(root.DomainEvents);
    }

    // RemoveDomainEvent remove apenas a instância indicada, preservando os demais eventos do buffer.
    [Fact]
    public void RemoveDomainEvent_DropsSpecificEvent()
    {
        var root = new TestAggregateRoot();
        var e1 = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "first");
        var e2 = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "second");
        root.RaiseEvent(e1);
        root.RaiseEvent(e2);

        root.RetractEvent(e1);

        Assert.Equal(new[] { e2 }, root.DomainEvents);
    }
}
