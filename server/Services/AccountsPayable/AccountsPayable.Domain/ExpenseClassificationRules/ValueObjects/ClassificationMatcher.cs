namespace AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Predicate VO that decides whether a given <see cref="Payable"/> is a candidate for a rule.
/// Criteria are <b>conjunctive</b> (AND) — all set fields must match. At least one criterion
/// must be set. Currency on amount-range is not enforced at this layer (assume single-tenant
/// currency consistency).
/// </summary>
public sealed class ClassificationMatcher : ValueObject
{
    public const int KEYWORD_MAX_LENGTH = 200;

    public SupplierId? SupplierId { get; }
    public string? Keyword { get; }
    public Money? MinAmount { get; }
    public Money? MaxAmount { get; }

    public ClassificationMatcher(
        SupplierId? supplierId = null,
        string? keyword = null,
        Money? minAmount = null,
        Money? maxAmount = null)
    {
        var hasSupplier = supplierId is not null && supplierId.Value.Value != Guid.Empty;
        var hasKeyword = !string.IsNullOrWhiteSpace(keyword);
        var hasRange = minAmount is not null || maxAmount is not null;

        if (!hasSupplier && !hasKeyword && !hasRange)
            throw ClassificationMatcherErrors.AtLeastOneCriterionRequired();

        string? normalizedKeyword = null;
        if (hasKeyword)
        {
            normalizedKeyword = keyword!.Trim();
            if (normalizedKeyword.Length > KEYWORD_MAX_LENGTH)
                throw ClassificationMatcherErrors.KeywordTooLong(KEYWORD_MAX_LENGTH);
        }

        if (minAmount is not null && maxAmount is not null
            && minAmount.Amount > maxAmount.Amount)
            throw ClassificationMatcherErrors.InvalidValueRange(minAmount.Amount, maxAmount.Amount);

        SupplierId = hasSupplier ? supplierId : null;
        Keyword = normalizedKeyword;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
    }

    /// <summary>Returns true when every set criterion holds against <paramref name="payable"/>.</summary>
    public bool Matches(Payable payable)
    {
        ArgumentNullException.ThrowIfNull(payable);

        if (SupplierId is { } sup && sup != payable.SupplierId)
            return false;

        if (Keyword is { Length: > 0 } kw
            && !payable.Description.Value.Contains(kw, StringComparison.OrdinalIgnoreCase))
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
        yield return Keyword;
        yield return MinAmount;
        yield return MaxAmount;
    }
}
