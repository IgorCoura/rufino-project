namespace AccountsPayable.Domain.SeedWork;

using System.Reflection;

/// <summary>
/// Canonical Value Object base. Concrete VOs inherit as <c>sealed class</c>,
/// validate inputs in the constructor (throwing via factory of errors), and
/// implement <see cref="GetEqualityComponents"/>. <b>Do not use <c>record</c> for VOs</b> —
/// equality semantics here are by listed components, not by all positional members.
/// </summary>
public abstract class ValueObject
{
    protected static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (left is null ^ right is null)
            return false;
        return left is null || left.Equals(right);
    }

    protected static bool NotEqualOperator(ValueObject? left, ValueObject? right)
        => !EqualOperator(left, right);

    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override string ToString()
    {
        var type = this.GetType();
        PropertyInfo[] propInfos = type.GetProperties();

        var values = propInfos.Select(x =>
        {
            var value = x.GetValue(this);

            if (value is not null && value.GetType().IsArray)
            {
                var array = value as object[];
                value = "null";
                if (array is not null)
                {
                    var rendered = "[";
                    foreach (var ar in array)
                    {
                        rendered += ar?.ToString();
                        if (!ReferenceEquals(ar, array.Last()))
                            rendered += ",";
                    }
                    rendered += "]";
                    value = rendered;
                }
            }

            value ??= "null";
            return $"{x.Name}:{value}";
        }).ToList();

        var result = "{";
        for (var i = 0; i < values.Count; i++)
        {
            result += values[i];
            if (i < values.Count - 1)
                result += ",";
        }
        result += "}";
        return result;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (this.GetType() != obj.GetType())
            return false;

        var other = (ValueObject)obj;
        return this.GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x is null ? 0 : x.GetHashCode())
            .Aggregate(17, (acc, h) => (acc * 31) ^ h);
    }

    public ValueObject GetCopy()
        => (this.MemberwiseClone() as ValueObject)
            ?? throw new InvalidOperationException("MemberwiseClone returned an unexpected type.");
}
