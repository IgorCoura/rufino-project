namespace EconomicCore.Domain.SeedWork;

public abstract class Entity<TId> where TId : struct, IEntityId<TId>
{
    private int? _requestedHashCode;
    private TId _Id;

    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }

    protected Entity()
    {
        _Id = TId.New();
    }

    protected Entity(TId id)
    {
        if (id.Equals(TId.Empty))
            throw SeedWorkErrors.EmptyId(this.GetType().Name);
        _Id = id;
    }

    public virtual TId Id
    {
        get => _Id;
        protected set
        {
            if (value.Equals(TId.Empty))
                throw SeedWorkErrors.EmptyId(this.GetType().Name);
            _Id = value;
        }
    }

    public bool IsTransient() => this.Id.Equals(TId.Empty);

    public override bool Equals(object? obj)
    {
        if (obj == null || obj is not Entity<TId>)
            return false;
        if (Object.ReferenceEquals(this, obj))
            return true;
        if (this.GetType() != obj.GetType())
            return false;

        Entity<TId> item = (Entity<TId>)obj;
        if (item.IsTransient() || this.IsTransient())
            return false;
        return item.Id.Equals(this.Id);
    }

    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            if (!_requestedHashCode.HasValue)
                _requestedHashCode = this.Id.GetHashCode() ^ 31;
            return _requestedHashCode.Value;
        }
        return base.GetHashCode();
    }

    public static bool operator ==(Entity<TId> left, Entity<TId> right)
    {
        if (Object.Equals(left, null))
            return Object.Equals(right, null);
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId> left, Entity<TId> right) => !(left == right);
}
