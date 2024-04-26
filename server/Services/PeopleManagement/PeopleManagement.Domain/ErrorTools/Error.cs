namespace PeopleManagement.Domain.ErrorTools;

public class Error : IEquatable<Error>
{
    public string Code { get; }

    public string Message { get; }

    public Dictionary<string, string> Properties { get; private set; } = [];

    public Error(string code, string message, object properties)
    {
        Code = code;
        Message = message;
        ConvertToDictionary(properties);
    }


    private void ConvertToDictionary(object properties)
    {
        var dictionary = new Dictionary<string, string>();
        foreach (PropertyInfo property in properties.GetType().GetProperties())
        {            
            dictionary[property.Name] = property.GetValue(properties)?.ToString() ?? "null";
        }
        Properties = dictionary;

    }

    public static bool operator ==(Error? a, Error? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(Error? a, Error? b) => !(a == b);

    public virtual bool Equals(Error? other)
    {
        if (other is null)
        {
            return false;
        }

        return Code == other.Code;
    }

    public override bool Equals(object? obj) => obj is Error error && Equals(error);

    public override int GetHashCode() => HashCode.Combine(Code, Message);

    public override string ToString() => $"{Code} : {Message}";
}

