namespace AccountsPayable.Domain.ChartOfAccounts.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class AccountType : Enumeration
{
    public static readonly AccountType Asset = new(1, "ASSET");
    public static readonly AccountType Liability = new(2, "LIABILITY");
    public static readonly AccountType Equity = new(3, "EQUITY");
    public static readonly AccountType Revenue = new(4, "REVENUE");
    public static readonly AccountType Expense = new(5, "EXPENSE");

    private AccountType(int id, string name) : base(id, name) { }
}
