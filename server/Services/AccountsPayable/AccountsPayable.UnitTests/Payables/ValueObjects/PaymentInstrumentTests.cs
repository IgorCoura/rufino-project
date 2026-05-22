namespace AccountsPayable.UnitTests.Payables.ValueObjects;

using System.Text;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// PaymentInstrument — hierarquia selada com 4 variantes concretas. Testa:
/// (a) construção válida de cada variante; (b) <c>Method</c> derivado;
/// (c) igualdade estrutural; (d) variantes diferentes nunca são iguais (garantido pelo GetType
/// em <c>ValueObject.Equals</c>); (e) guardas de null no construtor.
/// </summary>
public class PaymentInstrumentTests
{
    private static readonly LegalName LEGAL = new("Acme Brasil LTDA");
    private static readonly TaxId TAXID = new("59.199.597/0001-98");
    private static readonly PixKey PIXKEY = new("contato@acme.com.br", PixKeyType.Email);
    private const string VALID_43_BARCODE = "0019020120012740420006911978140000050000000";

    private static BarcodeDigits ValidBarcode()
    {
        var sum = 0;
        var weight = 2;
        for (var i = VALID_43_BARCODE.Length - 1; i >= 0; i--)
        {
            sum += (VALID_43_BARCODE[i] - '0') * weight;
            weight++;
            if (weight > 9) weight = 2;
        }
        var remainder = sum % 11;
        var dv = 11 - remainder;
        if (dv == 0 || dv == 10 || dv == 11) dv = 1;
        var full = VALID_43_BARCODE[..4] + dv + VALID_43_BARCODE[4..];
        return new BarcodeDigits(full);
    }

    private static EmvPayload ValidEmv()
    {
        var body = "00020126360014BR.GOV.BCB.PIX0114+5511999998888"
                   + "5204000053039865802BR5913FULANO DE TAL6008BRASILIA62070503***6304";
        ushort crc = 0xFFFF;
        var bytes = Encoding.ASCII.GetBytes(body);
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0) crc = (ushort)((crc << 1) ^ 0x1021);
                else crc <<= 1;
            }
        }
        return new EmvPayload(body + crc.ToString("X4"));
    }

    public class SupplierPixTransfer
    {
        // Construção válida preserva legalName, taxId e pixKey; Method = SupplierTransfer.
        [Fact]
        public void Constructor_Valid_ShouldBuildAndExposeMethod()
        {
            var inst = new SupplierPixTransferInstrument(LEGAL, TAXID, PIXKEY);

            Assert.Equal(LEGAL, inst.SupplierLegalName);
            Assert.Equal(TAXID, inst.SupplierTaxId);
            Assert.Equal(PIXKEY, inst.PixKey);
            Assert.Equal(PaymentMethod.SupplierTransfer, inst.Method);
        }

        // LegalName null lança ArgumentNullException (guarda de framework no abstract intermediário).
        [Fact]
        public void Constructor_NullLegalName_ShouldThrow()
            => Assert.Throws<ArgumentNullException>(() => new SupplierPixTransferInstrument(null!, TAXID, PIXKEY));

        // TaxId null lança ArgumentNullException.
        [Fact]
        public void Constructor_NullTaxId_ShouldThrow()
            => Assert.Throws<ArgumentNullException>(() => new SupplierPixTransferInstrument(LEGAL, null!, PIXKEY));

        // PixKey null lança ArgumentNullException.
        [Fact]
        public void Constructor_NullPixKey_ShouldThrow()
            => Assert.Throws<ArgumentNullException>(() => new SupplierPixTransferInstrument(LEGAL, TAXID, null!));

        // Igualdade estrutural por (legalName, taxId, pixKey).
        [Fact]
        public void Equality_SameComponents_ShouldBeEqual()
        {
            var a = new SupplierPixTransferInstrument(LEGAL, TAXID, PIXKEY);
            var b = new SupplierPixTransferInstrument(LEGAL, TAXID, PIXKEY);
            Assert.Equal(a, b);
        }
    }

    public class SupplierBankTransfer
    {
        // Construção válida preserva todos os campos; Method = SupplierTransfer.
        [Fact]
        public void Constructor_Valid_ShouldBuildAndExposeMethod()
        {
            var inst = new SupplierBankTransferInstrument(
                LEGAL, TAXID, "001", "0001", "123456-7", BankAccountType.Checking);

            Assert.Equal("001", inst.BankCode);
            Assert.Equal("0001", inst.Branch);
            Assert.Equal("123456-7", inst.AccountNumber);
            Assert.Equal(BankAccountType.Checking, inst.AccountType);
            Assert.Equal(PaymentMethod.SupplierTransfer, inst.Method);
        }

        // BankCode inválido reusa o erro AP.SBA02 do SupplierBankAccount.
        [Fact]
        public void Constructor_InvalidBankCode_ShouldThrow_SBA02()
        {
            var ex = Assert.Throws<DomainException>(() => new SupplierBankTransferInstrument(
                LEGAL, TAXID, "12", "0001", "123456-7", BankAccountType.Checking));
            Assert.Equal("AP.SBA02", ex.Id);
        }

        // AccountType null reusa o erro AP.SBA05.
        [Fact]
        public void Constructor_NullAccountType_ShouldThrow_SBA05()
        {
            var ex = Assert.Throws<DomainException>(() => new SupplierBankTransferInstrument(
                LEGAL, TAXID, "001", "0001", "123456-7", null!));
            Assert.Equal("AP.SBA05", ex.Id);
        }

        // Igualdade estrutural por (legalName, taxId, bankCode, branch, accountNumber, accountType).
        [Fact]
        public void Equality_SameComponents_ShouldBeEqual()
        {
            var a = new SupplierBankTransferInstrument(LEGAL, TAXID, "001", "0001", "123456-7", BankAccountType.Checking);
            var b = new SupplierBankTransferInstrument(LEGAL, TAXID, "001", "0001", "123456-7", BankAccountType.Checking);
            Assert.Equal(a, b);
        }
    }

    public class DynamicPix
    {
        // Construção válida preserva o EMV payload; Method = DynamicPix.
        [Fact]
        public void Constructor_Valid_ShouldBuildAndExposeMethod()
        {
            var emv = ValidEmv();
            var inst = new DynamicPixInstrument(emv);

            Assert.Equal(emv, inst.Payload);
            Assert.Equal(PaymentMethod.DynamicPix, inst.Method);
        }

        // Payload null lança ArgumentNullException.
        [Fact]
        public void Constructor_NullPayload_ShouldThrow()
            => Assert.Throws<ArgumentNullException>(() => new DynamicPixInstrument(null!));
    }

    public class BankSlip
    {
        // Construção com barcode válido preserva o code e expõe Method = BankSlip. DigitableLine
        // não vive no instrumento (derivável via Barcode.ToDigitableLine — evita redundância).
        [Fact]
        public void Constructor_WithBarcode_ShouldBuildAndExposeMethod()
        {
            var bc = ValidBarcode();
            var inst = new BankSlipInstrument(bc);

            Assert.Equal(bc, inst.Barcode);
            Assert.Equal(PaymentMethod.BankSlip, inst.Method);
        }

        // Barcode null lança ArgumentNullException.
        [Fact]
        public void Constructor_NullBarcode_ShouldThrow()
            => Assert.Throws<ArgumentNullException>(() => new BankSlipInstrument(null!));
    }

    public class CrossVariantEquality
    {
        // Variantes concretas diferentes nunca são iguais — ValueObject.Equals compara GetType().
        [Fact]
        public void DifferentConcreteVariants_ShouldNeverBeEqual()
        {
            PaymentInstrument pix = new SupplierPixTransferInstrument(LEGAL, TAXID, PIXKEY);
            PaymentInstrument bank = new SupplierBankTransferInstrument(LEGAL, TAXID, "001", "0001", "123456-7", BankAccountType.Checking);
            PaymentInstrument dyn = new DynamicPixInstrument(ValidEmv());
            PaymentInstrument slip = new BankSlipInstrument(ValidBarcode());

            Assert.NotEqual(pix, bank);
            Assert.NotEqual(pix, dyn);
            Assert.NotEqual(pix, slip);
            Assert.NotEqual(bank, dyn);
            Assert.NotEqual(bank, slip);
            Assert.NotEqual(dyn, slip);
        }
    }
}
