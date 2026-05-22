namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Payable foi entregue ao BC <c>PaymentExecution</c> para execução. A partir da Sprint 12.C carrega
/// a serialização flat do <c>PaymentInstrument</c> (snapshot da criação) — o PSP usa esses campos
/// para emitir o pagamento, sem precisar consultar o estado atual do <c>Supplier</c>.
/// <para>
/// Campos antigos <c>BankAccountId</c> e <c>Method</c> foram removidos: o método de pagamento e
/// os dados bancários já vêm dentro do instrumento serializado.
/// </para>
/// </summary>
public sealed record PayablePaymentRequested(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    SupplierId SupplierId,
    decimal AmountValue,
    string AmountCurrency,
    string InstrumentKind,
    string? SupplierLegalName,
    string? SupplierTaxIdValue,
    string? SupplierTaxIdType,
    string? PixKeyValue,
    string? PixKeyType,
    string? BankCode,
    string? Branch,
    string? AccountNumber,
    string? AccountType,
    string? EmvPayload,
    string? BarcodeDigits) : IDomainEvent;
