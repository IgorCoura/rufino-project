namespace AccountsPayable.Domain.ExpenseClassificationRules;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.ExpenseClassificationRules.Events;
using AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Aggregate Root holding a single auto-classification rule. Traditional snapshot-persisted.
/// The matching/ranking among multiple rules belongs to the <c>PayableAutoClassifier</c>
/// Domain Service — this Aggregate only owns its own data and lifecycle (Active/Inactive,
/// content updates, learning origin).
/// <para>
/// <b>Priority semantics</b>: lower numeric value = higher priority (1 wins over 5).
/// </para>
/// </summary>
public sealed class ExpenseClassificationRule : AggregateRoot<ExpenseClassificationRuleId>
{
    public TenantId TenantId { get; private set; }
    public ClassificationMatcher Match { get; private set; } = default!;
    public ClassificationAction Action { get; private set; } = default!;
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public UserId? LearnedFromUserId { get; private set; }

    private ExpenseClassificationRule() : base() { }

    private ExpenseClassificationRule(ExpenseClassificationRuleId id) : base(id) { }

    /// <summary>Factory for rules created by hand from the app (no learning origin).</summary>
    public static ExpenseClassificationRule CreateManual(
        ExpenseClassificationRuleId id,
        TenantId tenantId,
        ClassificationMatcher match,
        ClassificationAction action,
        int priority,
        DateTime occurredAt)
        => CreateInternal(id, tenantId, match, action, priority, learnedFromUserId: null, occurredAt);

    /// <summary>Factory for rules promoted by the system after N consistent manual classifications by <paramref name="learnedFromUserId"/>.</summary>
    public static ExpenseClassificationRule LearnFromHistory(
        ExpenseClassificationRuleId id,
        TenantId tenantId,
        ClassificationMatcher match,
        ClassificationAction action,
        int priority,
        UserId learnedFromUserId,
        DateTime occurredAt)
        => CreateInternal(id, tenantId, match, action, priority, learnedFromUserId, occurredAt);

    private static ExpenseClassificationRule CreateInternal(
        ExpenseClassificationRuleId id,
        TenantId tenantId,
        ClassificationMatcher match,
        ClassificationAction action,
        int priority,
        UserId? learnedFromUserId,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(action);
        if (priority < 1)
            throw ExpenseClassificationRuleErrors.PriorityMustBePositive(priority);

        var rule = new ExpenseClassificationRule(id)
        {
            TenantId = tenantId,
            Match = match,
            Action = action,
            Priority = priority,
            IsActive = true,
            LearnedFromUserId = learnedFromUserId,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        rule.AddDomainEvent(new ExpenseClassificationRuleCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: tenantId,
            RuleId: id,
            Priority: priority,
            IsActive: true,
            MatchSupplierId: match.SupplierId,
            MatchKeyword: match.Keyword,
            MatchMinAmount: match.MinAmount?.Amount,
            MatchMaxAmount: match.MaxAmount?.Amount,
            MatchAmountCurrency: match.MinAmount?.Currency.Name ?? match.MaxAmount?.Currency.Name,
            ActionChartOfAccountsId: action.Account.ChartOfAccountsId,
            ActionAccountId: action.Account.AccountId,
            ActionCostCenterId: action.CostCenterId,
            ActionAutoApprove: action.AutoApprove,
            LearnedFromUserId: learnedFromUserId));

        return rule;
    }

    public void Update(ClassificationMatcher match, ClassificationAction action, int priority, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(action);
        if (priority < 1)
            throw ExpenseClassificationRuleErrors.PriorityMustBePositive(priority);

        Match = match;
        Action = action;
        Priority = priority;
        UpdatedAt = occurredAt;

        AddDomainEvent(new ExpenseClassificationRuleUpdated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            RuleId: Id,
            Priority: priority,
            MatchSupplierId: match.SupplierId,
            MatchKeyword: match.Keyword,
            MatchMinAmount: match.MinAmount?.Amount,
            MatchMaxAmount: match.MaxAmount?.Amount,
            MatchAmountCurrency: match.MinAmount?.Currency.Name ?? match.MaxAmount?.Currency.Name,
            ActionChartOfAccountsId: action.Account.ChartOfAccountsId,
            ActionAccountId: action.Account.AccountId,
            ActionCostCenterId: action.CostCenterId,
            ActionAutoApprove: action.AutoApprove));
    }

    public void Activate(DateTime occurredAt)
    {
        if (IsActive)
            throw ExpenseClassificationRuleErrors.AlreadyActive();

        IsActive = true;
        UpdatedAt = occurredAt;

        AddDomainEvent(new ExpenseClassificationRuleActivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            RuleId: Id));
    }

    public void Deactivate(DateTime occurredAt)
    {
        if (!IsActive)
            throw ExpenseClassificationRuleErrors.AlreadyInactive();

        IsActive = false;
        UpdatedAt = occurredAt;

        AddDomainEvent(new ExpenseClassificationRuleDeactivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            RuleId: Id));
    }
}
