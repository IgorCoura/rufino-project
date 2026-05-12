namespace AccountsPayable.UnitTests.SeedWork._TestDoubles;

using AccountsPayable.Domain.SeedWork;

internal sealed class TestMoney : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public TestMoney(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
