namespace EconomicCore.Domain.Prospective.EconomicContracts.Events;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed record CommitmentsGenerated(
    Guid EventId,
    EconomicContractId ContractId,
    TenantId TenantId,
    int PeriodYear,
    int PeriodMonth,
    string Purpose,
    CommitmentId OutflowCommitmentId,
    CommitmentId InflowCommitmentId,
    decimal ExpectedAmountValue,
    string ExpectedAmountCurrency,
    DateTime OccurredAt) : IDomainEvent;
