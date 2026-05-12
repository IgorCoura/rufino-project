namespace AccountsPayable.UnitTests.ChartOfAccounts.Enumerations;

using AccountsPayable.Domain.ChartOfAccounts.Enumerations;
using AccountsPayable.Domain.SeedWork;

public class AccountTypeTests
{
    // GetAll retorna exatamente os 5 tipos contábeis (Asset, Liability, Equity, Revenue, Expense).
    [Fact]
    public void GetAll_ShouldReturnAllFiveAccountTypes()
    {
        var all = Enumeration.GetAll<AccountType>().ToList();

        Assert.Equal(5, all.Count);
        Assert.Contains(AccountType.Asset, all);
        Assert.Contains(AccountType.Liability, all);
        Assert.Contains(AccountType.Equity, all);
        Assert.Contains(AccountType.Revenue, all);
        Assert.Contains(AccountType.Expense, all);
    }
}
