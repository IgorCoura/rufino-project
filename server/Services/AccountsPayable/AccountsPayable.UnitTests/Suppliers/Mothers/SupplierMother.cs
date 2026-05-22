namespace AccountsPayable.UnitTests.Suppliers.Mothers;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public static class SupplierMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));

    // Algoritmicamente válidos.
    public const string VALID_CNPJ = "59.199.597/0001-98";
    public const string VALID_CPF = "135.675.460-07";

    public static Supplier Active(
        SupplierId? id = null,
        TenantId? tenantId = null,
        string legalName = "Acme Brasil LTDA",
        string? tradeName = "Acme",
        string taxId = VALID_CNPJ,
        string email = "contato@acme.com.br",
        DateTime? occurredAt = null)
    {
        return Supplier.Create(
            id: id ?? SupplierId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            legalName: new LegalName(legalName),
            tradeName: tradeName is null ? null : new TradeName(tradeName),
            taxId: new TaxId(taxId),
            contact: new ContactInfo(new EmailAddress(email)),
            occurredAt: occurredAt ?? DEFAULT_OCCURRED_AT);
    }

    public static SupplierBankTransferAccount BankTransferAccount(
        string bankCode = "001",
        string branch = "0001",
        string accountNumber = "123456-7",
        BankAccountType? accountType = null)
        => new(bankCode, branch, accountNumber, accountType ?? BankAccountType.Checking);

    public static SupplierPixAccount PixAccount(string? cnpj = null)
        => new(new PixKey(cnpj ?? VALID_CNPJ, PixKeyType.Cnpj));

    public static Supplier ActiveWithBankAccount(
        string bankCode = "001",
        string branch = "0001",
        string accountNumber = "123456-7",
        BankAccountType? accountType = null)
    {
        var supplier = Active();
        supplier.AddBankAccount(
            BankTransferAccount(bankCode, branch, accountNumber, accountType),
            occurredAt: DEFAULT_OCCURRED_AT.AddMinutes(1));
        return supplier;
    }

    public static Supplier ActiveWithPixAccount(string? cnpj = null)
    {
        var supplier = Active();
        supplier.AddBankAccount(PixAccount(cnpj), occurredAt: DEFAULT_OCCURRED_AT.AddMinutes(1));
        return supplier;
    }

    public static Supplier ActiveWithBankAccounts(int count = 2)
    {
        var supplier = Active();
        for (var i = 0; i < count; i++)
        {
            supplier.AddBankAccount(
                BankTransferAccount(bankCode: "001", branch: "0001", accountNumber: $"100{i:D3}-{i}"),
                occurredAt: DEFAULT_OCCURRED_AT.AddMinutes(i + 1));
        }
        return supplier;
    }

    public static Supplier Blocked(string reason = "Inadimplência")
    {
        var supplier = Active();
        supplier.Block(reason, DEFAULT_OCCURRED_AT.AddDays(1));
        return supplier;
    }

    public static Supplier Inactive()
    {
        var supplier = Active();
        supplier.Deactivate(DEFAULT_OCCURRED_AT.AddDays(1));
        return supplier;
    }
}
