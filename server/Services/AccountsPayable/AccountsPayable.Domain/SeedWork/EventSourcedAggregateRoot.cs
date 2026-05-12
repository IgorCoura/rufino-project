namespace AccountsPayable.Domain.SeedWork;

using System.Collections.Concurrent;
using System.Reflection;
using AccountsPayable.Domain.Errors;

/// <summary>
/// Base for Aggregate Roots persisted via Event Sourcing (decision D-405 — applies to <c>Payable</c>
/// in this BC, plus <c>PaymentOrder</c>, <c>JournalEntry</c>, <c>CapturedBill</c> in other BCs).
/// <para>
/// Lifecycle:
/// <list type="bullet">
///   <item>Concrete aggregate exposes a parameterless ctor + a factory <c>Initialize(...)</c> that calls
///         <see cref="Apply"/> with a creation event (e.g., <c>PayableCreated</c>) — this sets <see cref="Id"/>
///         inside the corresponding <c>When</c> handler.</item>
///   <item>For rehydration, concrete aggregate exposes a ctor accepting <c>IEnumerable&lt;IDomainEvent&gt;</c>
///         and forwards it to <see cref="EventSourcedAggregateRoot{TId}(IEnumerable{IDomainEvent})"/>.</item>
///   <item>Business methods call <see cref="Apply"/> — never assign properties directly outside <c>When</c> handlers.</item>
///   <item>Repository drains pending changes via <see cref="PullChanges"/> after a successful <c>AppendToStream</c>.</item>
/// </list>
/// </para>
/// </summary>
public abstract class EventSourcedAggregateRoot<TId> where TId : struct, IEntityId<TId>
{
    private const BindingFlags WHEN_BINDING_FLAGS =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly ConcurrentDictionary<(Type RootType, Type EventType), MethodInfo?> WHEN_METHOD_CACHE = new();

    private readonly List<IDomainEvent> _changes = [];

    public TId Id { get; protected set; } = TId.Empty;

    /// <summary>Number of events seen so far (rehydration + new <see cref="Apply"/> calls).
    /// Used as <c>expectedVersion</c> for optimistic concurrency on <c>AppendToStream</c>.</summary>
    public int Version { get; private set; }

    public IReadOnlyList<IDomainEvent> Changes => _changes.AsReadOnly();

    /// <summary>Ctor for new aggregates. First business method must call <see cref="Apply"/> with a creation event.</summary>
    protected EventSourcedAggregateRoot() { }

    /// <summary>Rehydration ctor: replays the event stream, mutating state without recording new changes.</summary>
    protected EventSourcedAggregateRoot(IEnumerable<IDomainEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        foreach (var @event in history)
        {
            Mutate(@event);
            Version++;
        }
    }

    /// <summary>Records a new event: mutates state via <c>When</c> and appends to <see cref="Changes"/>.</summary>
    protected void Apply(IDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        Mutate(@event);
        _changes.Add(@event);
        Version++;
    }

    /// <summary>Drains accumulated changes. Called by the EventStore-backed repository after a successful append.</summary>
    public IReadOnlyList<IDomainEvent> PullChanges()
    {
        var snapshot = _changes.ToList();
        _changes.Clear();
        return snapshot;
    }

    public void ClearChanges() => _changes.Clear();

    private void Mutate(IDomainEvent @event)
    {
        var key = (RootType: this.GetType(), EventType: @event.GetType());

        var method = WHEN_METHOD_CACHE.GetOrAdd(key, static k =>
            k.RootType.GetMethod(
                name: "When",
                bindingAttr: WHEN_BINDING_FLAGS,
                binder: null,
                types: new[] { k.EventType },
                modifiers: null));

        if (method is null)
            throw SeedWorkErrors.MissingWhenHandler(key.RootType.Name, key.EventType.Name);

        method.Invoke(this, new object[] { @event });
    }

    public override bool Equals(object? obj)
    {
        if (obj is not EventSourcedAggregateRoot<TId> other)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (this.GetType() != other.GetType())
            return false;
        if (Id.Equals(TId.Empty) || other.Id.Equals(TId.Empty))
            return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
        => Id.Equals(TId.Empty) ? base.GetHashCode() : Id.GetHashCode() ^ 31;

    public static bool operator ==(EventSourcedAggregateRoot<TId>? left, EventSourcedAggregateRoot<TId>? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(EventSourcedAggregateRoot<TId>? left, EventSourcedAggregateRoot<TId>? right)
        => !(left == right);
}
