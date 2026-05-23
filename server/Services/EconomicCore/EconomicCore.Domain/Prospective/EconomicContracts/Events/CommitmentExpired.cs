namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.SeedWork;

public sealed record CommitmentExpired(
    Guid EventId,
    EconomicContractId ContractId,
    CommitmentId CommitmentId,
    DateTime OccurredAt) : IDomainEvent;
