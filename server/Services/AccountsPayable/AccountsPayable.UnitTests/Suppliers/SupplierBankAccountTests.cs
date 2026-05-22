namespace AccountsPayable.UnitTests.Suppliers;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// SupplierBankAccount é Value Object selado polimórfico — duas variantes mutuamente exclusivas:
/// <see cref="SupplierPixAccount"/> e <see cref="SupplierBankTransferAccount"/>. Testes cobrem
/// validação de campos de cada variante, normalização e igualdade estrutural (incluindo a
/// invariante "variantes diferentes nunca são iguais", garantida pelo <c>GetType</c> em <c>ValueObject.Equals</c>).
/// </summary>
public class SupplierBankAccountTests
{
    public class WhenCreatingBankTransfer
    {
        // BankCode com formato não-numérico ou tamanho diferente de 3 lança AP.SBA02.
        [Theory]
        [InlineData("ABC")]
        [InlineData("01")]
        [InlineData("12345")]
        public void Constructor_WithInvalidBankCode_ShouldThrowDomainException(string bankCode)
        {
            var ex = Assert.Throws<DomainException>(() => new SupplierBankTransferAccount(
                bankCode, "0001", "123456-7", BankAccountType.Checking));

            Assert.Equal("AP.SBA02", ex.Id);
        }

        // BankCode vazio/em branco lança AP.SBA01 (vazio cai na guarda antes da validação de tamanho).
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyBankCode_ShouldThrowDomainException(string bankCode)
        {
            var ex = Assert.Throws<DomainException>(() => new SupplierBankTransferAccount(
                bankCode, "0001", "123456-7", BankAccountType.Checking));

            Assert.Equal("AP.SBA01", ex.Id);
        }

        // Branch vazio/em branco lança AP.SBA03.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyBranch_ShouldThrowDomainException(string branch)
        {
            var ex = Assert.Throws<DomainException>(() => new SupplierBankTransferAccount(
                "001", branch, "123456-7", BankAccountType.Checking));

            Assert.Equal("AP.SBA03", ex.Id);
        }

        // AccountNumber vazio/em branco lança AP.SBA04.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyAccountNumber_ShouldThrowDomainException(string accountNumber)
        {
            var ex = Assert.Throws<DomainException>(() => new SupplierBankTransferAccount(
                "001", "0001", accountNumber, BankAccountType.Checking));

            Assert.Equal("AP.SBA04", ex.Id);
        }

        // AccountType null lança AP.SBA05.
        [Fact]
        public void Constructor_WithNullAccountType_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => new SupplierBankTransferAccount(
                "001", "0001", "123456-7", null!));

            Assert.Equal("AP.SBA05", ex.Id);
        }

        // BankCode com separadores é normalizado para apenas dígitos quando há exatos 3.
        [Fact]
        public void Constructor_BankCodeWithSeparators_ShouldNormalizeToDigits()
        {
            var account = new SupplierBankTransferAccount(
                "0-0-1", "0001", "123456-7", BankAccountType.Checking);

            Assert.Equal("001", account.BankCode);
        }

        // Branch e AccountNumber têm trim aplicado nos espaços externos.
        [Fact]
        public void Constructor_BranchAndAccountNumber_ShouldBeTrimmed()
        {
            var account = new SupplierBankTransferAccount(
                "001", "  0001  ", "  123456-7  ", BankAccountType.Checking);

            Assert.Equal("0001", account.Branch);
            Assert.Equal("123456-7", account.AccountNumber);
        }
    }

    public class WhenCreatingPix
    {
        // PixKey null no construtor lança ArgumentNullException (guarda de framework, não DomainException).
        [Fact]
        public void Constructor_WithNullPixKey_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SupplierPixAccount(null!));
        }

        // PixKey válida é preservada no VO sem alteração.
        [Fact]
        public void Constructor_WithValidPixKey_ShouldStoreIt()
        {
            var key = new PixKey("59.199.597/0001-98", PixKeyType.Cnpj);

            var account = new SupplierPixAccount(key);

            Assert.Equal(key, account.PixKey);
        }
    }

    public class WhenComparing
    {
        // Duas SupplierBankTransferAccount com os mesmos componentes são iguais (igualdade estrutural).
        [Fact]
        public void TwoBankTransferAccounts_WithSameComponents_ShouldBeEqual()
        {
            var a = new SupplierBankTransferAccount("001", "0001", "123456-7", BankAccountType.Checking);
            var b = new SupplierBankTransferAccount("001", "0001", "123456-7", BankAccountType.Checking);

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        // Diferença em qualquer componente quebra igualdade entre duas SupplierBankTransferAccount.
        [Fact]
        public void TwoBankTransferAccounts_DifferingInAnyComponent_ShouldNotBeEqual()
        {
            var baseAcc = new SupplierBankTransferAccount("001", "0001", "123456-7", BankAccountType.Checking);

            Assert.NotEqual(baseAcc, new SupplierBankTransferAccount("237", "0001", "123456-7", BankAccountType.Checking));
            Assert.NotEqual(baseAcc, new SupplierBankTransferAccount("001", "0002", "123456-7", BankAccountType.Checking));
            Assert.NotEqual(baseAcc, new SupplierBankTransferAccount("001", "0001", "999999-9", BankAccountType.Checking));
            Assert.NotEqual(baseAcc, new SupplierBankTransferAccount("001", "0001", "123456-7", BankAccountType.Savings));
        }

        // Duas SupplierPixAccount com a mesma PixKey são iguais (igualdade delegada ao VO PixKey).
        [Fact]
        public void TwoPixAccounts_WithSameKey_ShouldBeEqual()
        {
            var a = new SupplierPixAccount(new PixKey("contato@acme.com.br", PixKeyType.Email));
            var b = new SupplierPixAccount(new PixKey("contato@acme.com.br", PixKeyType.Email));

            Assert.Equal(a, b);
        }

        // SupplierPixAccount e SupplierBankTransferAccount nunca são iguais entre si — ValueObject.Equals
        // compara GetType() antes dos componentes, então variantes diferentes da hierarquia divergem.
        [Fact]
        public void PixAccount_VS_BankTransferAccount_ShouldNeverBeEqual()
        {
            SupplierBankAccount pix = new SupplierPixAccount(new PixKey("59.199.597/0001-98", PixKeyType.Cnpj));
            SupplierBankAccount bank = new SupplierBankTransferAccount("001", "0001", "123456-7", BankAccountType.Checking);

            Assert.NotEqual(pix, bank);
        }
    }
}
