namespace AccountsPayable.Domain.Payables.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Monetary amount with explicit currency. Always strictly positive — Payable não admite
/// valor zero ou negativo. Para refunds/credit notes (futuro), criar VO específico.
/// Valor arredondado a 2 casas com banker's rounding.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        if (currency is null)
            throw MoneyErrors.CurrencyRequired();
        if (amount <= 0)
            throw MoneyErrors.NotPositive(amount);

        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
