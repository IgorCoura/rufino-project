namespace AccountsPayable.Domain.Payables.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class Currency : Enumeration
{
    public static readonly Currency Brl = new(1, "BRL");
    public static readonly Currency Usd = new(2, "USD");

    private Currency(int id, string name) : base(id, name) { }
}
