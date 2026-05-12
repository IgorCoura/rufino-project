namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.SeedWork;

public sealed record PayableClassified(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    AccountId AccountId,
    CostCenterId CostCenterId,
    UserId ClassifiedBy) : IDomainEvent;
