namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.ExpenseClassificationRules;
using AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;
using AccountsPayable.Domain.Payables;

/// <summary>
/// Stateless Domain Service that picks the best <see cref="ExpenseClassificationRule"/> for a
/// <see cref="Payable"/>. Filters out inactive and cross-tenant rules, evaluates the matcher of
/// each remaining rule, and returns the highest-priority hit (lowest numeric value wins).
/// <para>
/// The Application layer feeds in the list of candidate rules (it owns the repository query) and,
/// when a non-null decision comes back, calls <c>Payable.ClassifyAutomatically</c>. When the
/// decision is null, the payable stays in the human-review queue.
/// </para>
/// </summary>
public sealed class PayableAutoClassifier
{
    public ClassificationDecision? Decide(Payable payable, IReadOnlyList<ExpenseClassificationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(payable);
        ArgumentNullException.ThrowIfNull(rules);

        ExpenseClassificationRule? best = null;
        foreach (var rule in rules)
        {
            if (!rule.IsActive) continue;
            if (rule.TenantId != payable.TenantId) continue;
            if (!rule.Match.Matches(payable)) continue;

            if (best is null || rule.Priority < best.Priority)
                best = rule;
        }

        return best is null ? null : new ClassificationDecision(best.Id, best.Action);
    }
}

public sealed record ClassificationDecision(
    ExpenseClassificationRuleId RuleId,
    ClassificationAction Action);
