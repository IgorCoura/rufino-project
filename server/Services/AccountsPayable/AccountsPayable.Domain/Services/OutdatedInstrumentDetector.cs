namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// Stateless Domain Service da Sprint 12.E. Cruza o snapshot do <see cref="PaymentInstrument"/>
/// congelado no <see cref="Payable"/> com o estado atual do <see cref="Supplier"/> e retorna
/// quais campos divergem. A Application usa o resultado para chamar
/// <see cref="Payable.FlagInstrumentOutdated"/> (gravação no stream) e disparar notificação ao
/// usuário (proativo). DynamicPix e BankSlip são auto-contidos — sempre <see cref="OutdatedInstrumentReport.NotOutdated"/>.
/// </summary>
public sealed class OutdatedInstrumentDetector
{
    public OutdatedInstrumentReport Detect(Payable payable, Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(payable);
        ArgumentNullException.ThrowIfNull(supplier);

        if (payable.TenantId != supplier.TenantId || payable.SupplierId != supplier.Id)
            return OutdatedInstrumentReport.NotOutdated();

        if (payable.PaymentInstrument is not SupplierTransferInstrument transfer)
            return OutdatedInstrumentReport.NotOutdated();

        var divergent = new List<string>();

        if (!transfer.SupplierLegalName.Equals(supplier.LegalName))
            divergent.Add(nameof(SupplierTransferInstrument.SupplierLegalName));
        if (!transfer.SupplierTaxId.Equals(supplier.TaxId))
            divergent.Add(nameof(SupplierTransferInstrument.SupplierTaxId));

        var snapshotAccount = ToSupplierBankAccount(transfer);
        if (!supplier.ActiveBankAccounts.Contains(snapshotAccount))
            divergent.Add("BankAccount");

        return divergent.Count == 0
            ? OutdatedInstrumentReport.NotOutdated()
            : new OutdatedInstrumentReport(IsOutdated: true, DivergentFields: divergent.AsReadOnly());
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

/// <summary>
/// Resultado do <see cref="OutdatedInstrumentDetector.Detect"/> — sinaliza se há divergência e
/// lista os campos que mudaram. <see cref="Reason"/> é uma string já formatada para uso direto
/// como motivo em <see cref="Payable.FlagInstrumentOutdated"/>.
/// </summary>
public sealed record OutdatedInstrumentReport(bool IsOutdated, IReadOnlyList<string> DivergentFields)
{
    public static OutdatedInstrumentReport NotOutdated()
        => new(false, Array.Empty<string>());

    public string Reason => IsOutdated
        ? $"Snapshot diverge do Supplier atual em: {string.Join(", ", DivergentFields)}."
        : string.Empty;
}
