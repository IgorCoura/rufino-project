namespace AccountsPayable.Domain.Payables.ValueObjects;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Cross-aggregate reference to an <c>Account</c> (an internal Entity of <c>ChartOfAccounts</c>).
/// <para>
/// DDD rule (Vernon, IDDD ch. 10): a reference to an Entity that lives inside another Aggregate
/// must be anchored to that Aggregate's Root identity. A bare <see cref="ChartOfAccounts.Entities.AccountId"/>
/// is meaningless outside its owning chart — this VO carries both halves so the link is complete
/// and addressable without leaking the AR's internal scoping.
/// </para>
/// </summary>
public sealed class AccountRef : ValueObject
{
    public ChartOfAccountsId ChartOfAccountsId { get; }
    public AccountId AccountId { get; }

    public AccountRef(ChartOfAccountsId chartOfAccountsId, AccountId accountId)
    {
        if (chartOfAccountsId.Value == Guid.Empty)
            throw AccountRefErrors.ChartOfAccountsIdRequired();
        if (accountId.Value == Guid.Empty)
            throw AccountRefErrors.AccountIdRequired();

        ChartOfAccountsId = chartOfAccountsId;
        AccountId = accountId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ChartOfAccountsId;
        yield return AccountId;
    }
}
