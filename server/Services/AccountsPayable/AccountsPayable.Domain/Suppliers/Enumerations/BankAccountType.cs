namespace AccountsPayable.Domain.Suppliers.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class BankAccountType : Enumeration
{
    public static readonly BankAccountType Checking = new(1, "CHECKING");
    public static readonly BankAccountType Savings = new(2, "SAVINGS");
    public static readonly BankAccountType Salary = new(3, "SALARY");

    private BankAccountType(int id, string name) : base(id, name) { }
}
