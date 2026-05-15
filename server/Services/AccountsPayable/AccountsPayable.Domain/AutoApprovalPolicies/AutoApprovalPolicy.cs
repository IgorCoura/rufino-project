namespace AccountsPayable.Domain.AutoApprovalPolicies;

using AccountsPayable.Domain.AutoApprovalPolicies.Entities;
using AccountsPayable.Domain.AutoApprovalPolicies.Events;
using AccountsPayable.Domain.AutoApprovalPolicies.ValueObjects;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Aggregate Root holding a tenant's auto-approval policy (alçadas múltiplas). Traditional
/// snapshot-persisted. Contains a collection of internal <see cref="ApprovalRule"/> entities,
/// mutated only through this root.
/// <para>
/// The matching/ranking among multiple rules belongs to <c>ApprovalRequirementCalculator</c> —
/// this Aggregate only owns its own structure (which rules exist, which are active).
/// </para>
/// </summary>
public sealed class AutoApprovalPolicy : AggregateRoot<AutoApprovalPolicyId>
{
    private readonly List<ApprovalRule> _rules = [];

    public TenantId TenantId { get; private set; }

    public IReadOnlyList<ApprovalRule> Rules => _rules.AsReadOnly();

    private AutoApprovalPolicy() : base() { }

    private AutoApprovalPolicy(AutoApprovalPolicyId id) : base(id) { }

    public static AutoApprovalPolicy Create(
        AutoApprovalPolicyId id,
        TenantId tenantId,
        DateTime occurredAt)
    {
        var policy = new AutoApprovalPolicy(id)
        {
            TenantId = tenantId,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        policy.AddDomainEvent(new AutoApprovalPolicyCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: tenantId,
            PolicyId: id));

        return policy;
    }

    public ApprovalRule AddRule(
        ApprovalRuleId ruleId,
        ApprovalMatchCriteria matchCriteria,
        Money thresholdAmount,
        ApproverRoles requiredApproverRoles,
        int requiredApprovalCount,
        DateTime occurredAt)
    {
        var rule = new ApprovalRule(ruleId, matchCriteria, thresholdAmount, requiredApproverRoles, requiredApprovalCount, occurredAt);
        _rules.Add(rule);
        UpdatedAt = occurredAt;

        AddDomainEvent(new ApprovalRuleAdded(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            PolicyId: Id,
            RuleId: ruleId,
            MatchSupplierId: matchCriteria.SupplierId,
            MatchAccountId: matchCriteria.AccountId,
            MatchMinAmount: matchCriteria.MinAmount?.Amount,
            MatchMaxAmount: matchCriteria.MaxAmount?.Amount,
            MatchAmountCurrency: matchCriteria.MinAmount?.Currency.Name ?? matchCriteria.MaxAmount?.Currency.Name,
            ThresholdAmountValue: thresholdAmount.Amount,
            ThresholdAmountCurrency: thresholdAmount.Currency.Name,
            RequiredApproverRoles: requiredApproverRoles.Roles,
            RequiredApprovalCount: requiredApprovalCount));

        return rule;
    }

    public void RemoveRule(ApprovalRuleId ruleId, DateTime occurredAt)
    {
        var rule = FindRule(ruleId);
        _rules.Remove(rule);
        UpdatedAt = occurredAt;

        AddDomainEvent(new ApprovalRuleRemoved(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            PolicyId: Id,
            RuleId: ruleId));
    }

    public void ActivateRule(ApprovalRuleId ruleId, DateTime occurredAt)
    {
        var rule = FindRule(ruleId);
        rule.Activate(occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new ApprovalRuleActivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            PolicyId: Id,
            RuleId: ruleId));
    }

    public void DeactivateRule(ApprovalRuleId ruleId, DateTime occurredAt)
    {
        var rule = FindRule(ruleId);
        rule.Deactivate(occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new ApprovalRuleDeactivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            PolicyId: Id,
            RuleId: ruleId));
    }

    private ApprovalRule FindRule(ApprovalRuleId ruleId)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == ruleId)
            ?? throw AutoApprovalPolicyErrors.ApprovalRuleNotFound(ruleId.Value);
        return rule;
    }
}
