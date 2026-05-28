namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed record EconomicContractCreated(
    Guid EventId,
    EconomicContractId ContractId,
    TenantId TenantId,
    EconomicAgentId CounterpartyId,
    EconomicResourceId ResourceId,
    string DirectionName,
    string PeriodicityName,
    int AnchorDay,
    int TermMonths,
    DateOnly StartDate,
    decimal ExpectedAmountValue,
    string ExpectedAmountCurrency,
    decimal TolerancePercent,
    int WindowDays,
    DateTime OccurredAt) : IDomainEvent;
