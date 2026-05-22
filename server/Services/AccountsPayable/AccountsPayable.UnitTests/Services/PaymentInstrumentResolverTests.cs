namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Services;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;
using AccountsPayable.UnitTests.Payables.Mothers;
using AccountsPayable.UnitTests.Suppliers.Mothers;

/// <summary>
/// PaymentInstrumentResolver (Sprint 12.C) — Domain Service stateless que valida no momento do
/// RequestPayment se o snapshot do instrumento ainda bate com uma conta ativa do Supplier atual.
/// Aplica-se apenas a variantes SupplierTransferInstrument; DynamicPix/BankSlip são auto-contidas.
/// </summary>
public class PaymentInstrumentResolverTests
{
    private readonly PaymentInstrumentResolver _sut = new();

    // Constrói Supplier ativo com 1 conta bancária default + supplierId casando com PayableMother.DEFAULT_SUPPLIER.
    private static Supplier ActiveSupplierWithDefaultBankAccount()
    {
        var supplier = Supplier.Create(
            id: PayableMother.DEFAULT_SUPPLIER,
            tenantId: PayableMother.DEFAULT_TENANT,
            legalName: PayableMother.DEFAULT_SUPPLIER_LEGAL_NAME,
            tradeName: null,
            taxId: PayableMother.DEFAULT_SUPPLIER_TAX_ID,
            contact: new ContactInfo(new EmailAddress("contato@acme.com.br")),
            occurredAt: PayableMother.DEFAULT_OCCURRED_AT);
        supplier.AddBankAccount(
            new SupplierBankTransferAccount("001", "0001", "123456-7", BankAccountType.Checking),
            PayableMother.DEFAULT_OCCURRED_AT.AddMinutes(1));
        return supplier;
    }

    // Cenário feliz: snapshot do instrumento corresponde à conta ativa do Supplier → passa sem lançar.
    [Fact]
    public void EnsureSnapshot_WhenSnapshotMatchesActive_ShouldNotThrow()
    {
        var supplier = ActiveSupplierWithDefaultBankAccount();
        var payable = PayableMother.Draft();

        _sut.EnsureSnapshotMatchesActiveAccounts(payable, supplier);
    }

    // Snapshot não corresponde a nenhuma conta ativa do Supplier → AP.PAY21.
    [Fact]
    public void EnsureSnapshot_WhenSnapshotDoesNotMatchActives_ShouldThrow_PAY21()
    {
        var supplier = Supplier.Create(
            id: PayableMother.DEFAULT_SUPPLIER,
            tenantId: PayableMother.DEFAULT_TENANT,
            legalName: PayableMother.DEFAULT_SUPPLIER_LEGAL_NAME,
            tradeName: null,
            taxId: PayableMother.DEFAULT_SUPPLIER_TAX_ID,
            contact: new ContactInfo(new EmailAddress("contato@acme.com.br")),
            occurredAt: PayableMother.DEFAULT_OCCURRED_AT);
        supplier.AddBankAccount(
            new SupplierBankTransferAccount("237", "0002", "999999-9", BankAccountType.Savings),
            PayableMother.DEFAULT_OCCURRED_AT.AddMinutes(1));

        var payable = PayableMother.Draft();

        var ex = Assert.Throws<DomainException>(
            () => _sut.EnsureSnapshotMatchesActiveAccounts(payable, supplier));

        Assert.Equal("AP.PAY21", ex.Id);
    }

    // DynamicPixInstrument é auto-contido (EMV payload bruto) — Resolver não cruza com Supplier.
    [Fact]
    public void EnsureSnapshot_WithDynamicPix_ShouldNotCheckSupplierAccounts()
    {
        var supplier = ActiveSupplierWithDefaultBankAccount();
        var emv = ValidEmv();
        var payable = PayableMother.Draft(
            paymentInstrument: new DynamicPixInstrument(emv));

        _sut.EnsureSnapshotMatchesActiveAccounts(payable, supplier);
    }

    // BankSlipInstrument é auto-contido (código de barras) — Resolver não cruza com Supplier.
    [Fact]
    public void EnsureSnapshot_WithBankSlip_ShouldNotCheckSupplierAccounts()
    {
        var supplier = ActiveSupplierWithDefaultBankAccount();
        var bc = ValidBarcode();
        var payable = PayableMother.Draft(
            paymentInstrument: new BankSlipInstrument(bc));

        _sut.EnsureSnapshotMatchesActiveAccounts(payable, supplier);
    }

    // TenantId do Supplier diverge do TenantId do Payable → AP.PAY21 (proteção anti-IDOR).
    [Fact]
    public void EnsureSnapshot_WhenTenantsDiffer_ShouldThrow_PAY21()
    {
        var supplier = Supplier.Create(
            id: PayableMother.DEFAULT_SUPPLIER,
            tenantId: TenantId.From(new Guid("99999999-9999-9999-9999-999999999999")),
            legalName: PayableMother.DEFAULT_SUPPLIER_LEGAL_NAME,
            tradeName: null,
            taxId: PayableMother.DEFAULT_SUPPLIER_TAX_ID,
            contact: new ContactInfo(new EmailAddress("contato@acme.com.br")),
            occurredAt: PayableMother.DEFAULT_OCCURRED_AT);
        supplier.AddBankAccount(
            new SupplierBankTransferAccount("001", "0001", "123456-7", BankAccountType.Checking),
            PayableMother.DEFAULT_OCCURRED_AT.AddMinutes(1));

        var payable = PayableMother.Draft();

        var ex = Assert.Throws<DomainException>(
            () => _sut.EnsureSnapshotMatchesActiveAccounts(payable, supplier));

        Assert.Equal("AP.PAY21", ex.Id);
    }

    // SupplierId divergente → AP.PAY21 (não pode validar contra outro fornecedor).
    [Fact]
    public void EnsureSnapshot_WhenSupplierIdDiffers_ShouldThrow_PAY21()
    {
        var supplier = Supplier.Create(
            id: SupplierId.From(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            tenantId: PayableMother.DEFAULT_TENANT,
            legalName: PayableMother.DEFAULT_SUPPLIER_LEGAL_NAME,
            tradeName: null,
            taxId: PayableMother.DEFAULT_SUPPLIER_TAX_ID,
            contact: new ContactInfo(new EmailAddress("contato@acme.com.br")),
            occurredAt: PayableMother.DEFAULT_OCCURRED_AT);
        supplier.AddBankAccount(
            new SupplierBankTransferAccount("001", "0001", "123456-7", BankAccountType.Checking),
            PayableMother.DEFAULT_OCCURRED_AT.AddMinutes(1));

        var payable = PayableMother.Draft();

        var ex = Assert.Throws<DomainException>(
            () => _sut.EnsureSnapshotMatchesActiveAccounts(payable, supplier));

        Assert.Equal("AP.PAY21", ex.Id);
    }

    // ------- helpers -------

    private static EmvPayload ValidEmv()
    {
        var body = "00020126360014BR.GOV.BCB.PIX0114+5511999998888"
                   + "5204000053039865802BR5913FULANO DE TAL6008BRASILIA62070503***6304";
        ushort crc = 0xFFFF;
        var bytes = System.Text.Encoding.ASCII.GetBytes(body);
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

    private static BarcodeDigits ValidBarcode()
    {
        const string s43 = "0019020120012740420006911978140000050000000";
        var sum = 0;
        var weight = 2;
        for (var i = s43.Length - 1; i >= 0; i--)
        {
            sum += (s43[i] - '0') * weight;
            weight++;
            if (weight > 9) weight = 2;
        }
        var remainder = sum % 11;
        var dv = 11 - remainder;
        if (dv == 0 || dv == 10 || dv == 11) dv = 1;
        return new BarcodeDigits(s43[..4] + dv + s43[4..]);
    }
}
