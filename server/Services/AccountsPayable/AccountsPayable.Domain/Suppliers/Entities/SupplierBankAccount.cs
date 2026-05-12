namespace AccountsPayable.Domain.Suppliers.Entities;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// Internal Entity owned by <see cref="Supplier"/>. Mutated only via <c>internal</c> methods
/// invoked by the Aggregate Root. Does not emit Domain Events directly — the Root does.
/// </summary>
public sealed class SupplierBankAccount : Entity<SupplierBankAccountId>
{
    public const int BANK_CODE_LENGTH = 3;
    public const int BRANCH_MAX_LENGTH = 10;
    public const int ACCOUNT_NUMBER_MAX_LENGTH = 20;

    public string BankCode { get; private set; } = default!;
    public string Branch { get; private set; } = default!;
    public string AccountNumber { get; private set; } = default!;
    public BankAccountType AccountType { get; private set; } = default!;
    public PixKey? PixKey { get; private set; }

    private SupplierBankAccount() : base() { }

    internal SupplierBankAccount(
        SupplierBankAccountId id,
        string bankCode,
        string branch,
        string accountNumber,
        BankAccountType accountType,
        PixKey? pixKey,
        DateTime occurredAt) : base(id)
    {
        if (accountType is null)
            throw SupplierBankAccountErrors.AccountTypeRequired();

        SetBankCode(bankCode);
        SetBranch(branch);
        SetAccountNumber(accountNumber);
        AccountType = accountType;
        PixKey = pixKey;
        CreatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    private void SetBankCode(string bankCode)
    {
        if (string.IsNullOrWhiteSpace(bankCode))
            throw SupplierBankAccountErrors.BankCodeEmpty();
        var digits = new string(bankCode.Where(char.IsDigit).ToArray());
        if (digits.Length != BANK_CODE_LENGTH)
            throw SupplierBankAccountErrors.BankCodeInvalid(bankCode);
        BankCode = digits;
    }

    private void SetBranch(string branch)
    {
        if (string.IsNullOrWhiteSpace(branch))
            throw SupplierBankAccountErrors.BranchEmpty();
        Branch = branch.Trim();
    }

    private void SetAccountNumber(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw SupplierBankAccountErrors.AccountNumberEmpty();
        AccountNumber = accountNumber.Trim();
    }
}
