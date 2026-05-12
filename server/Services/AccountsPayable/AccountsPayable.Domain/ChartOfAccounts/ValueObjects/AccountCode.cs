namespace AccountsPayable.Domain.ChartOfAccounts.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Hierarchical accounting code: digits grouped by dots (e.g., <c>4</c>, <c>4.01</c>,
/// <c>4.01.01</c>). No leading/trailing dot, no consecutive dots, no other characters.
/// </summary>
public sealed class AccountCode : ValueObject
{
    public const int MAX_LENGTH = 50;

    public string Value { get; }

    public AccountCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw AccountCodeErrors.Empty();

        var trimmed = value.Trim();
        if (trimmed.Length > MAX_LENGTH)
            throw AccountCodeErrors.TooLong(MAX_LENGTH);
        if (!IsValidFormat(trimmed))
            throw AccountCodeErrors.InvalidFormat(trimmed);

        Value = trimmed;
    }

    private static bool IsValidFormat(string code)
    {
        if (code[0] == '.' || code[^1] == '.')
            return false;
        if (code.Contains(".."))
            return false;
        foreach (var c in code)
        {
            if (!char.IsDigit(c) && c != '.')
                return false;
        }
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
