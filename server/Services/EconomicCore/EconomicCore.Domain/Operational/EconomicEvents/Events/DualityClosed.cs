namespace EconomicCore.Domain.Operational.EconomicEvents.Events;

using EconomicCore.Domain.SeedWork;

public sealed record DualityClosed(
    Guid EventId,
    EconomicEventId EconomicEventId,
    EconomicEventId CounterpartEventId,
    decimal MatchedAmountValue,
    string MatchedAmountCurrency,
    DateTime OccurredAt) : IDomainEvent;
