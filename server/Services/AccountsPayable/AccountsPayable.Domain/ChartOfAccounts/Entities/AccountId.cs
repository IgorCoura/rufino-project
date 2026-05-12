namespace AccountsPayable.Domain.ChartOfAccounts.Entities;

using AccountsPayable.Domain.SeedWork;

public readonly record struct AccountId(Guid Value) : IEntityId<AccountId>
{
    public static AccountId New() => new(Guid.NewGuid());
    public static AccountId From(Guid value) => new(value);
    public static AccountId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
