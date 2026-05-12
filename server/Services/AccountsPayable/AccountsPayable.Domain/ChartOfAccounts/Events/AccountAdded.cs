namespace AccountsPayable.Domain.ChartOfAccounts.Events;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.SeedWork;

public sealed record AccountAdded(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ChartOfAccountsId ChartOfAccountsId,
    AccountId AccountId,
    AccountId? ParentId,
    string Code,
    string Name,
    string Type) : IDomainEvent;
