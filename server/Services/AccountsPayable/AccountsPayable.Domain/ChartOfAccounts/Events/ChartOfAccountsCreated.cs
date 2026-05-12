namespace AccountsPayable.Domain.ChartOfAccounts.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record ChartOfAccountsCreated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ChartOfAccountsId ChartOfAccountsId,
    string Name) : IDomainEvent;
