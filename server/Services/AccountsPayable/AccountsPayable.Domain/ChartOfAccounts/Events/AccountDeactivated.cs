namespace AccountsPayable.Domain.ChartOfAccounts.Events;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.SeedWork;

public sealed record AccountDeactivated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ChartOfAccountsId ChartOfAccountsId,
    AccountId AccountId) : IDomainEvent;
