namespace AccountsPayable.Domain.AutoApprovalPolicies.Entities;

using AccountsPayable.Domain.AutoApprovalPolicies.ValueObjects;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Internal Entity inside the <see cref="AutoApprovalPolicy"/> Aggregate. Mutated only through the
/// Aggregate Root — never created directly outside this BC. Holds one approval rule: criteria,
/// threshold, eligible approver roles and how many distinct approvals are required.
/// <para>
/// <b>Threshold semantics</b>: the rule applies only when <c>Payable.Amount &gt;= ThresholdAmount</c>.
/// Below the threshold the rule is out of scope (does not fire).
/// </para>
/// </summary>
public sealed class ApprovalRule : Entity<ApprovalRuleId>
{
    public ApprovalMatchCriteria MatchCriteria { get; private set; } = default!;
    public Money ThresholdAmount { get; private set; } = default!;
    public ApproverRoles RequiredApproverRoles { get; private set; } = default!;
    public int RequiredApprovalCount { get; private set; }
    public bool IsActive { get; private set; }

    private ApprovalRule() : base() { }

    internal ApprovalRule(
        ApprovalRuleId id,
        ApprovalMatchCriteria matchCriteria,
        Money thresholdAmount,
        ApproverRoles requiredApproverRoles,
        int requiredApprovalCount,
        DateTime occurredAt) : base(id)
    {
        ArgumentNullException.ThrowIfNull(matchCriteria);
        ArgumentNullException.ThrowIfNull(thresholdAmount);
        ArgumentNullException.ThrowIfNull(requiredApproverRoles);
        if (requiredApprovalCount < 1)
            throw AutoApprovalPolicyErrors.RequiredApprovalCountTooLow(requiredApprovalCount);

        MatchCriteria = matchCriteria;
        ThresholdAmount = thresholdAmount;
        RequiredApproverRoles = requiredApproverRoles;
        RequiredApprovalCount = requiredApprovalCount;
        IsActive = true;
        CreatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    internal void Activate(DateTime occurredAt)
    {
        if (IsActive)
            throw AutoApprovalPolicyErrors.ApprovalRuleAlreadyActive(Id.Value);
        IsActive = true;
        UpdatedAt = occurredAt;
    }

    internal void Deactivate(DateTime occurredAt)
    {
        if (!IsActive)
            throw AutoApprovalPolicyErrors.ApprovalRuleAlreadyInactive(Id.Value);
        IsActive = false;
        UpdatedAt = occurredAt;
    }
}
