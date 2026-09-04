namespace BillPayment.Domain.SharedKernel;

using BillPayment.Domain.SeedWork;

public sealed class Currency : Enumeration
{
    public static readonly Currency BRL = new(1, "BRL");

    private Currency(int id, string name) : base(id, name) { }
}
