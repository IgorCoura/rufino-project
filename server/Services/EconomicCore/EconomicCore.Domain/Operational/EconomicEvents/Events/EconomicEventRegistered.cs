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
    IReadOnlyList<EconomicEventCovering> Coverings,
    Guid? CounterpartEventId,
    DateTime OccurredAt) : IDomainEvent;

/// <summary>
/// One covering commitment carried by <see cref="EconomicEventRegistered"/>. A bundled payment carries one entry
/// per allocation; the deferred duality-close handler closes one leg per covering. Empty for a directly-paired event.
/// </summary>
public sealed record EconomicEventCovering(Guid ContractId, Guid CommitmentId);
