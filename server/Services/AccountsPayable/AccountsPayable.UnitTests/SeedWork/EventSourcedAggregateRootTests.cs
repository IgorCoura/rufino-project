namespace AccountsPayable.UnitTests.SeedWork;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.SeedWork._TestDoubles;

public class EventSourcedAggregateRootTests
{
    private static readonly DateTime Now = new(2026, 5, 12, 14, 0, 0, DateTimeKind.Utc);

    // Initialize aplica o evento de criação: popula Id e Name, incrementa Version para 1 e registra a mudança em Changes.
    [Fact]
    public void Initialize_AppliesCreationEvent_SetsIdAndRecordsChange()
    {
        var id = TestId.New();
        var agg = TestEventSourcedAggregate.Initialize(id, "first", Now);

        Assert.Equal(id, agg.Id);
        Assert.Equal("first", agg.Name);
        Assert.Equal(1, agg.Version);
        Assert.Single(agg.Changes);
        Assert.IsType<TestCreated>(agg.Changes[0]);
    }

    // Apply muta o estado via When e adiciona o evento em Changes, somando 1 em Version a cada chamada.
    [Fact]
    public void Apply_MutatesStateAndAppendsToChanges_IncrementingVersion()
    {
        var agg = TestEventSourcedAggregate.Initialize(TestId.New(), "first", Now);

        agg.Rename("second", Now.AddMinutes(1));

        Assert.Equal("second", agg.Name);
        Assert.Equal(2, agg.Version);
        Assert.Equal(2, agg.Changes.Count);
        Assert.IsType<TestRenamed>(agg.Changes[1]);
    }

    // PullChanges retorna snapshot dos eventos pendentes e esvazia Changes, mas Id/Name/Version permanecem.
    [Fact]
    public void PullChanges_DrainsBufferButKeepsState()
    {
        var id = TestId.New();
        var agg = TestEventSourcedAggregate.Initialize(id, "first", Now);
        agg.Rename("second", Now.AddMinutes(1));

        var pulled = agg.PullChanges();

        Assert.Equal(2, pulled.Count);
        Assert.Empty(agg.Changes);
        Assert.Equal("second", agg.Name);
        Assert.Equal(id, agg.Id);
        Assert.Equal(2, agg.Version);
    }

    // Rehydrate replaya o stream, reconstrói o estado final e mantém Changes vazio (replay não é mudança nova).
    [Fact]
    public void Rehydrate_ReplaysHistory_RebuildsStateWithoutRecordingChanges()
    {
        var id = TestId.New();
        var history = new IDomainEvent[]
        {
            new TestCreated(Guid.NewGuid(), Now, id, "first"),
            new TestRenamed(Guid.NewGuid(), Now.AddMinutes(1), "second"),
            new TestRenamed(Guid.NewGuid(), Now.AddMinutes(2), "third"),
        };

        var agg = TestEventSourcedAggregate.Rehydrate(history);

        Assert.Equal(id, agg.Id);
        Assert.Equal("third", agg.Name);
        Assert.Equal(3, agg.Version);
        Assert.Empty(agg.Changes);
    }

    // Rehydrate com stream vazio deixa o Aggregate no estado default (Id=Empty, Version=0, Name=null).
    [Fact]
    public void Rehydrate_FromEmptyStream_LeavesAggregateInDefaultState()
    {
        var agg = TestEventSourcedAggregate.Rehydrate(Array.Empty<IDomainEvent>());

        Assert.Equal(TestId.Empty, agg.Id);
        Assert.Equal(0, agg.Version);
        Assert.Null(agg.Name);
        Assert.Empty(agg.Changes);
    }

    // Aplicar um evento que o Aggregate não trata em When(...) lança SWK02 (handler ausente é programming error de domínio).
    [Fact]
    public void Apply_EventWithoutWhenHandler_Throws_SWK02()
    {
        var agg = TestEventSourcedAggregate.Initialize(TestId.New(), "first", Now);

        var ex = Assert.Throws<DomainException>(() => agg.ApplyUnhandled(Now));
        Assert.Equal("SWK02", ex.Id);
    }

    // Dois Event-Sourced Aggregates do mesmo tipo com o mesmo Id são iguais (Equals e ==).
    [Fact]
    public void Equality_TwoInstancesWithSameId_AreEqual()
    {
        var id = TestId.New();
        var a = TestEventSourcedAggregate.Initialize(id, "x", Now);
        var b = TestEventSourcedAggregate.Initialize(id, "y", Now);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    // Ctor de reidratação rejeita null como histórico — guarda básica contra chamada incorreta do repositório.
    [Fact]
    public void Apply_NullEvent_Throws()
    {
        var agg = TestEventSourcedAggregate.Initialize(TestId.New(), "x", Now);
        // Apply is protected; null is checked at the public façade. We just verify the
        // rehydration ctor also guards against null history.
        Assert.Throws<ArgumentNullException>(() => TestEventSourcedAggregate.Rehydrate(null!));
    }
}
