namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.AutoApprovalPolicies;
using AccountsPayable.Domain.AutoApprovalPolicies.Entities;
using AccountsPayable.Domain.Payables;

/// <summary>
/// Stateless Domain Service that computes the <see cref="ApprovalRequirement"/> for a
/// <see cref="Payable"/> against the tenant's <see cref="AutoApprovalPolicy"/>. The Application
/// layer reads the result and decides whether to call <c>Payable.Schedule</c> directly,
/// <c>Payable.RequestApproval</c> (single-approver), or <c>Payable.RequestMultiApproval(...)</c>.
/// <para>
/// <b>Selection logic</b>:
/// <list type="number">
///   <item>Keep only active rules whose <c>MatchCriteria.Matches(payable)</c> is true AND whose
///         <c>ThresholdAmount</c> the payable meets or exceeds.</item>
///   <item>Among the survivors, pick the one with the highest <c>RequiredApprovalCount</c> — "most
///         restrictive wins" (Sprint 10 critério de aceite).</item>
///   <item>If no rule survives, fall back to <see cref="ApprovalRequirement.DefaultManual"/>
///         (always require one manual approval).</item>
/// </list>
/// </para>
/// </summary>
public sealed class ApprovalRequirementCalculator
{
    public ApprovalRequirement Calculate(Payable payable, AutoApprovalPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(payable);
        ArgumentNullException.ThrowIfNull(policy);

        ApprovalRule? winner = null;
        foreach (var rule in policy.Rules)
        {
            if (!rule.IsActive) continue;
            if (payable.Amount.Amount < rule.ThresholdAmount.Amount) continue;
            if (!rule.MatchCriteria.Matches(payable)) continue;

            if (winner is null || rule.RequiredApprovalCount > winner.RequiredApprovalCount)
                winner = rule;
        }

        if (winner is null)
            return ApprovalRequirement.DefaultManual;

        return new ApprovalRequirement(
            Required: true,
            RequiredCount: winner.RequiredApprovalCount,
            EligibleRoles: winner.RequiredApproverRoles.Roles);
    }
}

public sealed record ApprovalRequirement(
    bool Required,
    int RequiredCount,
    IReadOnlyList<string> EligibleRoles)
{
    /// <summary>Default for payables not covered by any rule — one manual approval with no role restriction.</summary>
    public static readonly ApprovalRequirement DefaultManual = new(
        Required: true,
        RequiredCount: 1,
        EligibleRoles: new[] { "DEFAULT" });
}
