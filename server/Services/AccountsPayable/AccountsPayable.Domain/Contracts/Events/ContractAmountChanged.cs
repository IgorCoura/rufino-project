namespace AccountsPayable.Domain.Contracts.Events;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Versioned amount change — old value preserved in the event stream so historical recurrence
/// projections can be replayed accurately (alguma `ExpectedRecurringBill` antiga foi gerada com
/// o `OldAmountValue`). Currency is always preserved between revisions of the same contract.
/// </summary>
public sealed record ContractAmountChanged(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ContractId ContractId,
    decimal OldAmountValue,
    decimal NewAmountValue,
    string AmountCurrency,
    DateOnly EffectiveDate) : IDomainEvent;
