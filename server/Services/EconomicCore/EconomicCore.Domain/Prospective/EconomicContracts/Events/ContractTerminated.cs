namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed record ContractTerminated(
    Guid EventId,
    EconomicContractId ContractId,
    TenantId TenantId,
    DateTime OccurredAt) : IDomainEvent;
