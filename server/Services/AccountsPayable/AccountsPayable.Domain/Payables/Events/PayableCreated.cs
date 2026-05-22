namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Payable cadastrado manualmente. A partir da Sprint 12.B, o evento carrega o método de pagamento
/// (<see cref="PaymentMethodName"/>) e a serialização flat do <see cref="ValueObjects.PaymentInstrument"/>
/// — <see cref="InstrumentKind"/> + campos nullables por variante. Reidratação via
/// <c>PaymentInstrumentSerialization.Rebuild</c>.
/// </summary>
public sealed record PayableCreated(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    TenantId TenantId,
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
