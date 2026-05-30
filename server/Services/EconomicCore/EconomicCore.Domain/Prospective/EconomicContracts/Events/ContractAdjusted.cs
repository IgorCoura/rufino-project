namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

/// <summary>
/// Emitted when a mid-term adjustment (reajuste) re-prices the still-open commitments of a charge track from a
/// given competence onward. Past/settled commitments keep their locked amount (Value Pattern LockValue).
/// </summary>
public sealed record ContractAdjusted(
    Guid EventId,
    EconomicContractId ContractId,
    TenantId TenantId,
    string Purpose,
    int EffectiveFromYear,
    int EffectiveFromMonth,
    decimal NewAmountValue,
    string NewAmountCurrency,
    int RepricedCount,
    DateTime OccurredAt) : IDomainEvent;
