namespace AccountsPayable.Domain.ChartOfAccounts.Entities;

using AccountsPayable.Domain.ChartOfAccounts.Enumerations;
using AccountsPayable.Domain.ChartOfAccounts.ValueObjects;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Internal Entity owned by <see cref="ChartOfAccounts"/>. Mutated only via <c>internal</c>
/// methods invoked by the Aggregate Root. The Root owns tree-level invariants
/// (code uniqueness, parent presence, depth limit, deactivation rules).
/// </summary>
public sealed class Account : Entity<AccountId>
{
    public const int MAX_DEPTH = 5;

    public AccountId? ParentId { get; private set; }
    public AccountCode Code { get; private set; } = default!;
    public AccountName Name { get; private set; } = default!;
    public AccountType Type { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private Account() : base() { }

    internal Account(
        AccountId id,
        AccountId? parentId,
        AccountCode code,
        AccountName name,
        AccountType type,
        DateTime occurredAt) : base(id)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(name);
        if (type is null)
            throw ChartOfAccountsErrors.AccountTypeRequired();

        ParentId = parentId;
        Code = code;
        Name = name;
        Type = type;
        IsActive = true;
        CreatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    internal void Rename(AccountName newName, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(newName);
        Name = newName;
        UpdatedAt = occurredAt;
    }

    internal void Deactivate(DateTime occurredAt)
    {
        IsActive = false;
        UpdatedAt = occurredAt;
    }
}
