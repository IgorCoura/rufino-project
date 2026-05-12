namespace AccountsPayable.Domain.Payables.ValueObjects;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Calendar due date (no time-of-day). Contextual validation ("not in the past at creation")
/// lives in <c>Payable.Initialize</c> — the VO itself has no concept of "now".
/// </summary>
public sealed class DueDate : ValueObject
{
    public DateOnly Value { get; }

    public DueDate(DateOnly value)
    {
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
