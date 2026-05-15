namespace AccountsPayable.Domain.ExpenseClassificationRules.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record ExpenseClassificationRuleDeactivated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ExpenseClassificationRuleId RuleId) : IDomainEvent;
