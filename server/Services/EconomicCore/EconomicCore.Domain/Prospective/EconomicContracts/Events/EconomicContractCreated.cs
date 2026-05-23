namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed record EconomicContractCreated(
    Guid EventId,
    EconomicContractId ContractId,
    TenantId TenantId,
    EconomicAgentId CounterpartyId,
    string DirectionName,
    string PeriodicityName,
    int AnchorDay,
    decimal ExpectedAmountValue,
    string ExpectedAmountCurrency,
    decimal TolerancePercent,
    int WindowDays,
    DateTime OccurredAt) : IDomainEvent;
