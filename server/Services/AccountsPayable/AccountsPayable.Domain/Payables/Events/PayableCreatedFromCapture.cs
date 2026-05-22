namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Payable cadastrado a partir de uma <c>CapturedBill</c> (sibling BC <c>BillIngestion</c>).
/// A partir da Sprint 12.B carrega também o método+instrumento extraídos pela captura.
/// </summary>
public sealed record PayableCreatedFromCapture(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    TenantId TenantId,
    CapturedBillId CapturedBillId,
    SupplierId SupplierId,
    decimal AmountValue,
    string AmountCurrency,
    DateOnly DueDate,
    string Description,
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
