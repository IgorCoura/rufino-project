namespace AccountsPayable.Domain.Suppliers.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class PixKeyType : Enumeration
{
    public static readonly PixKeyType Cpf = new(1, "CPF");
    public static readonly PixKeyType Cnpj = new(2, "CNPJ");
    public static readonly PixKeyType Email = new(3, "EMAIL");
    public static readonly PixKeyType Phone = new(4, "PHONE");
    public static readonly PixKeyType Random = new(5, "RANDOM");

    private PixKeyType(int id, string name) : base(id, name) { }
}
