namespace AccountsPayable.Domain.ChartOfAccounts;

using AccountsPayable.Domain.SeedWork;

public readonly record struct ChartOfAccountsId(Guid Value) : IEntityId<ChartOfAccountsId>
{
    public static ChartOfAccountsId New() => new(Guid.NewGuid());
    public static ChartOfAccountsId From(Guid value) => new(value);
    public static ChartOfAccountsId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
