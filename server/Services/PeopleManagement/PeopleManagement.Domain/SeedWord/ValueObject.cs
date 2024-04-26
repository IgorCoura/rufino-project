using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.SeedWord;

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

            if(value != null && value.GetType().IsArray)
            {
                Object[]? array = value as Object[];
                value = "null";
                if (array != null)
                {
                    value = "[";
                    foreach (var ar in array)
                    {
                        value += ar.ToString();
                        if (ar != array.Last())
                            value += ",";
                    }
                    value += "]";
                }
                
            }

            value ??= "null";

            return $"{x.Name}:{value}";
        });

        var result = "{";
        foreach (var value in values)
        {
            result += value;
            if (value != values.Last())
                result += ",";
        }
        result += "}";
        return result;
    }


    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

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
        return this.MemberwiseClone() as ValueObject ?? throw new NullReferenceException();
    }

    public static Result<TValue> CreateCatchingException<TValue>(Func<TValue> createObject) where TValue : ValueObject
    {
        try
        {
            TValue result = createObject();
            return Result.Success<TValue>(result);
        }
        catch(DomainException e)
        {
            return Result.Failure<TValue>(e);
        }
    }
}
