namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.Services;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;
using AccountsPayable.UnitTests.Payables.Mothers;

/// <summary>
/// OutdatedInstrumentDetector (Sprint 12.E) — diagnostica divergência entre snapshot do Payable e
/// estado atual do Supplier. Retorna lista de campos divergentes e motivo formatado.
/// </summary>
public class OutdatedInstrumentDetectorTests
{
    private readonly OutdatedInstrumentDetector _sut = new();

    // Snapshot bate com tudo no Supplier atual → IsOutdated = false, sem divergentes.
    [Fact]
    public void Detect_WhenSnapshotMatchesCurrent_ShouldReportNotOutdated()
    {
        var supplier = SupplierWithDefaultAccount();
        var payable = PayableMother.Draft();

        var report = _sut.Detect(payable, supplier);

        Assert.False(report.IsOutdated);
        Assert.Empty(report.DivergentFields);
    }

    // Supplier renomeado após criação do Payable → IsOutdated = true, divergente em SupplierLegalName.
    [Fact]
    public void Detect_WhenLegalNameChanged_ShouldReportDivergentLegalName()
    {
        var supplier = SupplierWithDefaultAccount();
        supplier.Rename(new LegalName("Novo Nome SA"), PayableMother.DEFAULT_OCCURRED_AT.AddDays(5));
        var payable = PayableMother.Draft();

        var report = _sut.Detect(payable, supplier);

        Assert.True(report.IsOutdated);
        Assert.Contains("SupplierLegalName", report.DivergentFields);
    }

    // Conta bancária do snapshot foi desativada → divergente em BankAccount.
    [Fact]
    public void Detect_WhenSnapshotAccountDeactivated_ShouldReportDivergentBankAccount()
    {
        var supplier = SupplierWithDefaultAccount();
        // Adiciona uma segunda conta antes (pra poder desativar a primeira sem violar invariante de "última").
        var extraAccount = new SupplierBankTransferAccount("237", "0002", "999999-9", BankAccountType.Checking);
        supplier.AddBankAccount(extraAccount, PayableMother.DEFAULT_OCCURRED_AT.AddMinutes(2));
        var snapshotAccount = new SupplierBankTransferAccount("001", "0001", "123456-7", BankAccountType.Checking);
        supplier.DeactivateBankAccount(snapshotAccount, PayableMother.DEFAULT_OCCURRED_AT.AddDays(5));

        var payable = PayableMother.Draft();
        var report = _sut.Detect(payable, supplier);

        Assert.True(report.IsOutdated);
        Assert.Contains("BankAccount", report.DivergentFields);
    }

    // Payable com DynamicPixInstrument é auto-contido — Detector retorna NotOutdated sem cruzar com Supplier.
    [Fact]
    public void Detect_WithDynamicPix_ShouldReportNotOutdated()
    {
        var supplier = SupplierWithDefaultAccount();
        var emv = ValidEmv();
        var payable = PayableMother.Draft(
            paymentInstrument: new DynamicPixInstrument(emv));

        var report = _sut.Detect(payable, supplier);

        Assert.False(report.IsOutdated);
    }

    // Reason traz formato humano-readable com lista dos campos divergentes (use direto em FlagInstrumentOutdated).
    [Fact]
    public void Detect_WhenOutdated_ShouldFormatReasonForHumans()
    {
        var supplier = SupplierWithDefaultAccount();
        supplier.Rename(new LegalName("Acme Renomeada SA"), PayableMother.DEFAULT_OCCURRED_AT.AddDays(5));
        var payable = PayableMother.Draft();

        var report = _sut.Detect(payable, supplier);

        Assert.Contains("Snapshot diverge", report.Reason);
        Assert.Contains("SupplierLegalName", report.Reason);
    }

    // TenantId divergente (cross-tenant) → não emite report válido (retorna NotOutdated por segurança).
    [Fact]
    public void Detect_WhenCrossTenant_ShouldReportNotOutdated()
    {
        var supplier = SupplierMissingTenantMatch();
        var payable = PayableMother.Draft();

        var report = _sut.Detect(payable, supplier);

        Assert.False(report.IsOutdated);
    }

    // ------- helpers -------

    private static Supplier SupplierWithDefaultAccount()
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

    private static Supplier SupplierMissingTenantMatch()
    {
        var supplier = Supplier.Create(
            id: PayableMother.DEFAULT_SUPPLIER,
            tenantId: AccountsPayable.Domain.SeedWork.TenantId.From(new Guid("99999999-9999-9999-9999-999999999999")),
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
}
