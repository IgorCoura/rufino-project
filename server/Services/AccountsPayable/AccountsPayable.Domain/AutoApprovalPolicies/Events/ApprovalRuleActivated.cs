namespace AccountsPayable.Domain.AutoApprovalPolicies.Events;

using AccountsPayable.Domain.AutoApprovalPolicies.Entities;
using AccountsPayable.Domain.SeedWork;

public sealed record ApprovalRuleActivated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    AutoApprovalPolicyId PolicyId,
    ApprovalRuleId RuleId) : IDomainEvent;
