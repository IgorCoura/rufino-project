namespace EconomicCore.Domain.SharedKernel;

using EconomicCore.Domain.SeedWork;

public sealed class TaxIdKind : Enumeration
{
    public static readonly TaxIdKind CPF = new(1, "CPF", expectedLength: 11);
    public static readonly TaxIdKind CNPJ = new(2, "CNPJ", expectedLength: 14);

    public int ExpectedLength { get; }

    private TaxIdKind(int id, string name, int expectedLength) : base(id, name)
    {
        ExpectedLength = expectedLength;
    }
}
