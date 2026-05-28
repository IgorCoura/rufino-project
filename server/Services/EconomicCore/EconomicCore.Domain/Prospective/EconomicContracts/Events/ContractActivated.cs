namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed record ContractActivated(
    Guid EventId,
    EconomicContractId ContractId,
    TenantId TenantId,
    int TermMonths,
    DateTime OccurredAt) : IDomainEvent;
