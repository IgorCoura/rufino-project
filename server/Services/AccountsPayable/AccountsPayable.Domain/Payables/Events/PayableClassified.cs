namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Manual classification of a <see cref="Payable"/>. Carries both halves of the cross-aggregate
/// reference to <c>Account</c>: <see cref="ChartOfAccountsId"/> (the owning Aggregate Root) and
/// <see cref="AccountId"/> (the Entity inside it). Storing only <see cref="AccountId"/> would
/// violate the DDD rule "reference other aggregates only by the root's identity".
/// </summary>
public sealed record PayableClassified(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    ChartOfAccountsId ChartOfAccountsId,
    AccountId AccountId,
    CostCenterId CostCenterId,
    UserId ClassifiedBy) : IDomainEvent;
