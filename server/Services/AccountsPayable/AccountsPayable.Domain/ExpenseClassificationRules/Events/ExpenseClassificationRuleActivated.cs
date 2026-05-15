namespace AccountsPayable.Domain.ExpenseClassificationRules.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record ExpenseClassificationRuleActivated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ExpenseClassificationRuleId RuleId) : IDomainEvent;
