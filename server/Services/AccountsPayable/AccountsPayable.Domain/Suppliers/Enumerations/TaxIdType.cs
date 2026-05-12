namespace AccountsPayable.Domain.Suppliers.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class TaxIdType : Enumeration
{
    public static readonly TaxIdType Cpf = new(1, "CPF");
    public static readonly TaxIdType Cnpj = new(2, "CNPJ");

    private TaxIdType(int id, string name) : base(id, name) { }
}
