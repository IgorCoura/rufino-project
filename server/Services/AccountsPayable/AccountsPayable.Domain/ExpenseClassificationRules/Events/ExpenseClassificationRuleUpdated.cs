namespace AccountsPayable.Domain.ExpenseClassificationRules.Events;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public sealed record ExpenseClassificationRuleUpdated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ExpenseClassificationRuleId RuleId,
    int Priority,
    SupplierId? MatchSupplierId,
    string? MatchKeyword,
    decimal? MatchMinAmount,
    decimal? MatchMaxAmount,
    string? MatchAmountCurrency,
    ChartOfAccountsId ActionChartOfAccountsId,
    AccountId ActionAccountId,
    CostCenterId ActionCostCenterId,
    bool ActionAutoApprove) : IDomainEvent;
