namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

/// <summary>
/// Emitted when the contract's late-payment penalty policy (fine + interest) is replaced. Penalty commitments
/// already materialized keep the amount priced under the previous policy — the materialized obligation stands.
/// </summary>
public sealed record ContractPenaltyTermsChanged(
    Guid EventId,
    EconomicContractId ContractId,
    TenantId TenantId,
    string FineKindName,
    decimal FineValue,
    string InterestKindName,
    decimal InterestValue,
    string InterestPeriodName,
    DateTime OccurredAt) : IDomainEvent;
