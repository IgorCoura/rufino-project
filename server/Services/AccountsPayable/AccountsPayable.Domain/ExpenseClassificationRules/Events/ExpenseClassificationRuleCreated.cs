namespace AccountsPayable.Domain.ExpenseClassificationRules.Events;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Single event for both factories — <c>CreateManual</c> and <c>LearnFromHistory</c>. When
/// <see cref="LearnedFromUserId"/> is set, the rule was promoted by the system after a series of
/// consistent manual classifications (the count/threshold lives in Application).
/// </summary>
public sealed record ExpenseClassificationRuleCreated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ExpenseClassificationRuleId RuleId,
    int Priority,
    bool IsActive,
    SupplierId? MatchSupplierId,
    string? MatchKeyword,
    decimal? MatchMinAmount,
    decimal? MatchMaxAmount,
    string? MatchAmountCurrency,
    AccountId ActionAccountId,
    CostCenterId ActionCostCenterId,
    bool ActionAutoApprove,
    UserId? LearnedFromUserId) : IDomainEvent;
