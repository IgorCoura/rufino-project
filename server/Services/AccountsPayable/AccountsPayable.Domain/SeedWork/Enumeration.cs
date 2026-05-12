namespace AccountsPayable.Domain.SeedWork;

using System.Reflection;

/// <summary>
/// Canonical Smart Enum base. Concrete enums inherit as <c>sealed class</c> with
/// <c>public static readonly</c> instances. <b>Do not use the native C# <c>enum</c></b>
/// for domain concepts — use this so behavior (e.g., <c>CanTransitionTo</c>) lives on
/// the enum itself.
/// </summary>
public abstract class Enumeration : IComparable
{
    public string Name { get; private set; }
    public int Id { get; private set; }

    protected Enumeration(int id, string name) => (Id, Name) = (id, name);

    public override string ToString() => Name;

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T).GetFields(BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
                 .Select(f => f.GetValue(null))
                 .OfType<T>();

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration otherValue)
            return false;

        var typeMatches = GetType().Equals(obj.GetType());
        var valueMatches = Id.Equals(otherValue.Id);

        return typeMatches && valueMatches;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue)
        => Math.Abs(firstValue.Id - secondValue.Id);

    public static T FromValue<T>(int value) where T : Enumeration
        => Parse<T, int>(value, "value", item => item.Id == value);

    public static T? TryFromValue<T>(int value) where T : Enumeration
        => TryParse<T>(item => item.Id == value);

    public static T FromDisplayName<T>(string displayName) where T : Enumeration
        => Parse<T, string>(displayName, "display name",
            item => item.Name.Equals(displayName, StringComparison.CurrentCultureIgnoreCase));

    public static T? TryFromDisplayName<T>(string displayName) where T : Enumeration
        => TryParse<T>(item => item.Name.Equals(displayName, StringComparison.CurrentCultureIgnoreCase));

    private static T Parse<T, TKey>(TKey value, string description, Func<T, bool> predicate)
        where T : Enumeration
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);
        return matchingItem
            ?? throw new InvalidOperationException(
                $"'{value}' is not a valid {description} in {typeof(T)}");
    }

    private static T? TryParse<T>(Func<T, bool> predicate) where T : Enumeration
        => GetAll<T>().FirstOrDefault(predicate);

    public int CompareTo(object? other) => Id.CompareTo(((Enumeration?)other)?.Id);
}
