namespace AccountsPayable.UnitTests.Suppliers;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Entities;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.UnitTests.Suppliers.Mothers;

/// <summary>
/// SupplierBankAccount é Entity interna — só é testada via Root (Supplier.AddBankAccount).
/// </summary>
public class SupplierBankAccountTests
{
    private static readonly DateTime FIXED_NOW = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    // BankCode com formato não-numérico ou com tamanho diferente de 3 lança AP.SBA02.
    [Theory]
    [InlineData("ABC")]
    [InlineData("01")]
    [InlineData("12345")]
    public void AddBankAccount_WithInvalidBankCode_ShouldThrowDomainException(string bankCode)
    {
        var supplier = SupplierMother.Active();

        var ex = Assert.Throws<DomainException>(() => supplier.AddBankAccount(
            SupplierBankAccountId.New(), bankCode, "0001", "123456-7", BankAccountType.Checking, null, FIXED_NOW));

        Assert.Equal("AP.SBA02", ex.Id);
    }

    // BankCode vazio lança AP.SBA01 (vazio cai na guarda antes da validação de tamanho).
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddBankAccount_WithEmptyBankCode_ShouldThrowDomainException(string bankCode)
    {
        var supplier = SupplierMother.Active();

        var ex = Assert.Throws<DomainException>(() => supplier.AddBankAccount(
            SupplierBankAccountId.New(), bankCode, "0001", "123456-7", BankAccountType.Checking, null, FIXED_NOW));

        Assert.Equal("AP.SBA01", ex.Id);
    }

    // Branch vazio lança AP.SBA03.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddBankAccount_WithEmptyBranch_ShouldThrowDomainException(string branch)
    {
        var supplier = SupplierMother.Active();

        var ex = Assert.Throws<DomainException>(() => supplier.AddBankAccount(
            SupplierBankAccountId.New(), "001", branch, "123456-7", BankAccountType.Checking, null, FIXED_NOW));

        Assert.Equal("AP.SBA03", ex.Id);
    }

    // AccountNumber vazio lança AP.SBA04.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddBankAccount_WithEmptyAccountNumber_ShouldThrowDomainException(string accountNumber)
    {
        var supplier = SupplierMother.Active();

        var ex = Assert.Throws<DomainException>(() => supplier.AddBankAccount(
            SupplierBankAccountId.New(), "001", "0001", accountNumber, BankAccountType.Checking, null, FIXED_NOW));

        Assert.Equal("AP.SBA04", ex.Id);
    }

    // BankCode é normalizado para apenas dígitos (aceita "001-X" → "001" se houver 3 dígitos).
    [Fact]
    public void AddBankAccount_BankCodeWithSeparators_ShouldNormalizeToDigits()
    {
        var supplier = SupplierMother.Active();

        var account = supplier.AddBankAccount(
            SupplierBankAccountId.New(), "0-0-1", "0001", "123456-7", BankAccountType.Checking, null, FIXED_NOW);

        Assert.Equal("001", account.BankCode);
    }
}
