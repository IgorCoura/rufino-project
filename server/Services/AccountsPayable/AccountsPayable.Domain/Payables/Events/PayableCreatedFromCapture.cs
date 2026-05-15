namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Variant of <see cref="PayableCreated"/> emitted when the <see cref="Payable"/> originates from a
/// <c>CapturedBill</c> approved by the sibling <c>BillIngestion</c> BC. Carries the same payload
/// plus <see cref="CapturedBillId"/> so the link back to the originating capture survives in the
/// event stream and dedup checks (Application/Infra) can be performed by Id.
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
    string Description) : IDomainEvent;
