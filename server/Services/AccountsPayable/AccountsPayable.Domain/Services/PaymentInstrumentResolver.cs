namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// Stateless Domain Service que valida coerência cross-aggregate entre o snapshot do
/// <see cref="PaymentInstrument"/> de um <see cref="Payable"/> e o estado atual do
/// <see cref="Supplier"/>. Aplicável apenas para variantes <see cref="SupplierTransferInstrument"/>
/// (PIX e Bank) — <c>DynamicPix</c> e <c>BankSlip</c> são auto-contidas (payload bruto, sem
/// referência ao Supplier além do <c>SupplierId</c>).
/// <para>
/// Para casos de Supplier desatualizado em fluxo (detecção proativa por divergência), ver o
/// <c>OutdatedInstrumentDetector</c> da Sprint 12.E. Este Resolver é o gate síncrono final,
/// chamado pela Application antes de despachar para o PSP.
/// </para>
/// </summary>
public sealed class PaymentInstrumentResolver
{
    public void EnsureSnapshotMatchesActiveAccounts(Payable payable, Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(payable);
        ArgumentNullException.ThrowIfNull(supplier);

        if (payable.TenantId != supplier.TenantId)
            throw PayableErrors.BankAccountSupplierMismatch();
        if (payable.SupplierId != supplier.Id)
            throw PayableErrors.BankAccountSupplierMismatch();

        if (payable.PaymentInstrument is not SupplierTransferInstrument transfer)
            return;

        var snapshotAccount = ToSupplierBankAccount(transfer);
        if (!supplier.ActiveBankAccounts.Contains(snapshotAccount))
            throw PayableErrors.BankAccountSupplierMismatch();
    }

    private static SupplierBankAccount ToSupplierBankAccount(SupplierTransferInstrument transfer)
        => transfer switch
        {
            SupplierPixTransferInstrument p
                => new SupplierPixAccount(p.PixKey),
            SupplierBankTransferInstrument b
                => new SupplierBankTransferAccount(b.BankCode, b.Branch, b.AccountNumber, b.AccountType),
            _ => throw new InvalidOperationException(
                $"Variante de SupplierTransferInstrument desconhecida: {transfer.GetType().Name}."),
        };
}
