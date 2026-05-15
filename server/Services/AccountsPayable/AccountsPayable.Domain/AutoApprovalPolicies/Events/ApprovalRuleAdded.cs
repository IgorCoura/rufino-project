namespace AccountsPayable.Domain.AutoApprovalPolicies.Events;

using AccountsPayable.Domain.AutoApprovalPolicies.Entities;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public sealed record ApprovalRuleAdded(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    AutoApprovalPolicyId PolicyId,
    ApprovalRuleId RuleId,
    SupplierId? MatchSupplierId,
    AccountId? MatchAccountId,
    decimal? MatchMinAmount,
    decimal? MatchMaxAmount,
    string? MatchAmountCurrency,
    decimal ThresholdAmountValue,
    string ThresholdAmountCurrency,
    IReadOnlyList<string> RequiredApproverRoles,
    int RequiredApprovalCount) : IDomainEvent;
