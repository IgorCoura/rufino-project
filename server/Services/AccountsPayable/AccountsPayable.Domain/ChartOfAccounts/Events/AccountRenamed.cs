namespace AccountsPayable.Domain.ChartOfAccounts.Events;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.SeedWork;

public sealed record AccountRenamed(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ChartOfAccountsId ChartOfAccountsId,
    AccountId AccountId,
    string OldName,
    string NewName) : IDomainEvent;
