namespace EconomicCore.Domain.Operational.EconomicEvents.Events;

using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed record EconomicEventRegistered(
    Guid EventId,
    EconomicEventId EconomicEventId,
    TenantId TenantId,
    string DirectionName,
    EconomicResourceId ResourceId,
    decimal AmountValue,
    string AmountCurrency,
    int CompetenceYear,
    int CompetenceMonth,
    Guid? CoveringCommitmentId,
    Guid? CounterpartEventId,
    DateTime OccurredAt) : IDomainEvent;
