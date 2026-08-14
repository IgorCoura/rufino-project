namespace TenantManagement.Domain.SeedWork;

using System.Reflection;

public abstract class ValueObject
{
    protected static bool EqualOperator(ValueObject left, ValueObject right)
    {
        if (left is null ^ right is null)
        {
            return false;
        }
        return left is null || left.Equals(right);
    }

    protected static bool NotEqualOperator(ValueObject left, ValueObject right)
    {
        return !(EqualOperator(left, right));
    }

    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override string ToString()
    {
        Type t = this.GetType();

        PropertyInfo[] propInfos = t.GetProperties();
        var values = propInfos.Select(x =>
        {
            var value = x.GetValue(this);
            value ??= "null";
            return $"{x.Name}:{value}";
        });

        return "{" + string.Join(",", values) + "}";
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        if (obj.GetType() != this.GetType())
            return false;

        var other = (ValueObject)obj;
        return this.GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);
    }

    public ValueObject GetCopy()
    {
        return this.MemberwiseClone() as ValueObject ?? throw new InvalidOperationException("Copy failed.");
    }
}
