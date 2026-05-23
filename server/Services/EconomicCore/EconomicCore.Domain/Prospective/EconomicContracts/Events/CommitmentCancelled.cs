namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.SeedWork;

public sealed record CommitmentCancelled(
    Guid EventId,
    EconomicContractId ContractId,
    CommitmentId CommitmentId,
    DateTime OccurredAt) : IDomainEvent;
