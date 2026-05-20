namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.ExpenseClassificationRules;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Emitted when a <see cref="Payable"/> is classified by the <c>PayableAutoClassifier</c> Domain
/// Service (Sprint 9). Differs from <see cref="PayableClassified"/> by carrying the originating
/// <see cref="RuleId"/> instead of a human <c>UserId</c> — the event stream itself records whether
/// the classification was manual or automatic. Both halves of the <c>Account</c> reference travel
/// with the event (see <see cref="PayableClassified"/> for the DDD rationale).
/// </summary>
public sealed record PayableAutoClassified(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    ChartOfAccountsId ChartOfAccountsId,
    AccountId AccountId,
    CostCenterId CostCenterId,
    ExpenseClassificationRuleId RuleId) : IDomainEvent;
