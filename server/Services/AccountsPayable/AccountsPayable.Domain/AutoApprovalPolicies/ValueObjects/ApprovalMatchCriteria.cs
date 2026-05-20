namespace AccountsPayable.Domain.AutoApprovalPolicies.ValueObjects;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Predicate VO that decides whether an <c>ApprovalRule</c> is in scope for a given
/// <see cref="Payable"/>. Criteria are conjunctive (AND); at least one criterion must be set.
/// Similar in shape to <c>ClassificationMatcher</c> but semantically distinct: this drives the
/// approval policy, not the classification.
/// </summary>
public sealed class ApprovalMatchCriteria : ValueObject
{
    public SupplierId? SupplierId { get; }
    public AccountId? AccountId { get; }
    public Money? MinAmount { get; }
    public Money? MaxAmount { get; }

    public ApprovalMatchCriteria(
        SupplierId? supplierId = null,
        AccountId? accountId = null,
        Money? minAmount = null,
        Money? maxAmount = null)
    {
        var hasSupplier = supplierId is not null && supplierId.Value.Value != Guid.Empty;
        var hasAccount = accountId is not null && accountId.Value.Value != Guid.Empty;
        var hasRange = minAmount is not null || maxAmount is not null;

        if (!hasSupplier && !hasAccount && !hasRange)
            throw ApprovalMatchCriteriaErrors.AtLeastOneCriterionRequired();

        if (minAmount is not null && maxAmount is not null
            && minAmount.Amount > maxAmount.Amount)
            throw ApprovalMatchCriteriaErrors.InvalidValueRange(minAmount.Amount, maxAmount.Amount);

        SupplierId = hasSupplier ? supplierId : null;
        AccountId = hasAccount ? accountId : null;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
    }

    public bool Matches(Payable payable)
    {
        ArgumentNullException.ThrowIfNull(payable);

        if (SupplierId is { } sup && sup != payable.SupplierId)
            return false;

        if (AccountId is { } acc && (payable.Classification is null || payable.Classification.AccountId != acc))
            return false;

        if (MinAmount is not null && payable.Amount.Amount < MinAmount.Amount)
            return false;

        if (MaxAmount is not null && payable.Amount.Amount > MaxAmount.Amount)
            return false;

        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SupplierId;
        yield return AccountId;
        yield return MinAmount;
        yield return MaxAmount;
    }
}
