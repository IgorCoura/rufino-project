namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Marca que o snapshot do <c>PaymentInstrument</c> do Payable está desatualizado em relação ao
/// estado atual do <c>Supplier</c>. Emitido pelo comando <c>Payable.FlagInstrumentOutdated</c> em
/// reação a um <c>SupplierBankAccountDeactivated</c> (handler na Application — Sprint 12.E).
/// <para>
/// Idempotente por evento: o Payable emite uma única vez ao longo de sua vida. Chamadas
/// subsequentes ao comando são no-op silenciosas. Auditoria detalhada (quando exatamente o
/// Supplier mudou) vive no stream do próprio <c>Supplier</c> (eventos Add/Deactivate).
/// </para>
/// </summary>
public sealed record PayableInstrumentOutdated(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    string Reason) : IDomainEvent;
