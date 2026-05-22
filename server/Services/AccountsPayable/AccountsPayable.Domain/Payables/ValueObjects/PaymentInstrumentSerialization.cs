namespace AccountsPayable.Domain.Payables.ValueObjects;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// Serialização flat de <see cref="PaymentInstrument"/> para event payloads. O <c>Payable</c> é
/// Event-Sourced (D-405), então o instrumento precisa ser totalmente reconstruível a partir do
/// stream — daí o padrão "campos flat com <see cref="Payload.Kind"/> discriminator + campos
/// nullables por variante", consistente com os demais eventos do BC.
/// </summary>
internal static class PaymentInstrumentSerialization
{
    public const string KIND_SUPPLIER_PIX = "SUPPLIER_PIX";
    public const string KIND_SUPPLIER_BANK = "SUPPLIER_BANK";
    public const string KIND_DYNAMIC_PIX = "DYNAMIC_PIX";
    public const string KIND_BANK_SLIP = "BANK_SLIP";

    public static (string Kind,
                   string? SupplierLegalName, string? SupplierTaxIdValue, string? SupplierTaxIdType,
                   string? PixKeyValue, string? PixKeyType,
                   string? BankCode, string? Branch, string? AccountNumber, string? AccountType,
                   string? EmvPayload, string? BarcodeDigits)
        Expand(PaymentInstrument instrument)
    {
        ArgumentNullException.ThrowIfNull(instrument);

        return instrument switch
        {
            SupplierPixTransferInstrument p => (
                KIND_SUPPLIER_PIX,
                p.SupplierLegalName.Value, p.SupplierTaxId.Value, p.SupplierTaxId.Type.Name,
                p.PixKey.Value, p.PixKey.Type.Name,
                null, null, null, null,
                null, null),

            SupplierBankTransferInstrument b => (
                KIND_SUPPLIER_BANK,
                b.SupplierLegalName.Value, b.SupplierTaxId.Value, b.SupplierTaxId.Type.Name,
                null, null,
                b.BankCode, b.Branch, b.AccountNumber, b.AccountType.Name,
                null, null),

            DynamicPixInstrument d => (
                KIND_DYNAMIC_PIX,
                null, null, null,
                null, null,
                null, null, null, null,
                d.Payload.Value, null),

            BankSlipInstrument s => (
                KIND_BANK_SLIP,
                null, null, null,
                null, null,
                null, null, null, null,
                null, s.Barcode.Value),

            _ => throw new InvalidOperationException(
                $"Variant desconhecida de PaymentInstrument: {instrument.GetType().Name}."),
        };
    }

    public static PaymentInstrument Rebuild(
        string kind,
        string? supplierLegalName,
        string? supplierTaxIdValue,
        string? supplierTaxIdType,
        string? pixKeyValue,
        string? pixKeyType,
        string? bankCode,
        string? branch,
        string? accountNumber,
        string? accountType,
        string? emvPayload,
        string? barcodeDigits)
        => kind switch
        {
            KIND_SUPPLIER_PIX => new SupplierPixTransferInstrument(
                new LegalName(supplierLegalName!),
                new TaxId(supplierTaxIdValue!),
                new PixKey(pixKeyValue!, Enumeration.FromDisplayName<PixKeyType>(pixKeyType!))),

            KIND_SUPPLIER_BANK => new SupplierBankTransferInstrument(
                new LegalName(supplierLegalName!),
                new TaxId(supplierTaxIdValue!),
                bankCode!,
                branch!,
                accountNumber!,
                Enumeration.FromDisplayName<BankAccountType>(accountType!)),

            KIND_DYNAMIC_PIX => new DynamicPixInstrument(new EmvPayload(emvPayload!)),

            KIND_BANK_SLIP => new BankSlipInstrument(new BarcodeDigits(barcodeDigits!)),

            _ => throw new InvalidOperationException(
                $"Kind desconhecido de PaymentInstrument: {kind}."),
        };
}
