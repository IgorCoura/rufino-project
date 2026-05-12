namespace AccountsPayable.Domain.SeedWork;

using AccountsPayable.Domain.Errors;

/// <summary>
/// Base for any domain Entity with a strongly-typed Id. Aggregate Roots inherit
/// <see cref="AggregateRoot{TId}"/>; internal entities inherit this directly.
/// Does NOT carry Domain Events — those live in <see cref="AggregateRoot{TId}"/>.
/// </summary>
public abstract class Entity<TId> where TId : struct, IEntityId<TId>
{
    private int? _requestedHashCode;
    private TId _id;

    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }

    protected Entity()
    {
        _id = TId.New();
    }

    protected Entity(TId id)
    {
        if (id.Equals(TId.Empty))
            throw SeedWorkErrors.EmptyId(this.GetType().Name);
        _id = id;
    }

    public virtual TId Id
    {
        get => _id;
        protected set
        {
            if (value.Equals(TId.Empty))
                throw SeedWorkErrors.EmptyId(this.GetType().Name);
            _id = value;
        }
    }

    public bool IsTransient() => Id.Equals(TId.Empty);

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (this.GetType() != other.GetType())
            return false;
        if (other.IsTransient() || this.IsTransient())
            return false;
        return other.Id.Equals(this.Id);
    }

    public override int GetHashCode()
    {
        if (IsTransient())
            return base.GetHashCode();

        _requestedHashCode ??= Id.GetHashCode() ^ 31;
        return _requestedHashCode.Value;
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
