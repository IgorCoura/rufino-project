namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.SeedWork;

public sealed record CommitmentFulfilled(
    Guid EventId,
    EconomicContractId ContractId,
    CommitmentId CommitmentId,
    EconomicEventId FulfillingEventId,
    DateTime OccurredAt) : IDomainEvent;
