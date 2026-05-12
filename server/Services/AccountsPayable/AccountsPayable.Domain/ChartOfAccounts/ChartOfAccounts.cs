namespace AccountsPayable.Domain.ChartOfAccounts;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.ChartOfAccounts.Enumerations;
using AccountsPayable.Domain.ChartOfAccounts.Events;
using AccountsPayable.Domain.ChartOfAccounts.ValueObjects;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Aggregate Root representing the tenant's chart of accounts (a tree of accounting accounts).
/// Traditional snapshot persistence. The Aggregate IS the whole tree — accounts are mutated via
/// methods on the Root so tree-wide invariants (unique code, depth ≤ <see cref="Account.MAX_DEPTH"/>,
/// no deactivation while active children exist) stay consistent.
/// </summary>
public sealed class ChartOfAccounts : AggregateRoot<ChartOfAccountsId>
{
    private readonly List<Account> _accounts = [];

    public TenantId TenantId { get; private set; }
    public ChartOfAccountsName Name { get; private set; } = default!;

    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

    private ChartOfAccounts() : base() { }

    private ChartOfAccounts(ChartOfAccountsId id) : base(id) { }

    public static ChartOfAccounts Create(
        ChartOfAccountsId id,
        TenantId tenantId,
        ChartOfAccountsName name,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(name);

        var chart = new ChartOfAccounts(id)
        {
            TenantId = tenantId,
            Name = name,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        chart.AddDomainEvent(new ChartOfAccountsCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: tenantId,
            ChartOfAccountsId: id,
            Name: name.Value));

        return chart;
    }

    public Account AddAccount(
        AccountId id,
        AccountId? parentId,
        AccountCode code,
        AccountName name,
        AccountType type,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(name);
        if (type is null)
            throw ChartOfAccountsErrors.AccountTypeRequired();

        if (_accounts.Any(a => a.Code.Equals(code)))
            throw ChartOfAccountsErrors.DuplicatedAccountCode(code.Value);

        if (parentId.HasValue)
        {
            var parent = _accounts.FirstOrDefault(a => a.Id.Equals(parentId.Value))
                ?? throw ChartOfAccountsErrors.ParentNotFound(parentId.Value.Value);

            if (!parent.IsActive)
                throw ChartOfAccountsErrors.ParentInactive(parentId.Value.Value);

            var parentDepth = ComputeDepth(parent);
            if (parentDepth + 1 > Account.MAX_DEPTH)
                throw ChartOfAccountsErrors.MaxDepthExceeded(Account.MAX_DEPTH);
        }

        var account = new Account(id, parentId, code, name, type, occurredAt);
        _accounts.Add(account);
        UpdatedAt = occurredAt;

        AddDomainEvent(new AccountAdded(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            ChartOfAccountsId: Id,
            AccountId: id,
            ParentId: parentId,
            Code: code.Value,
            Name: name.Value,
            Type: type.Name));

        return account;
    }

    public void RenameAccount(AccountId accountId, AccountName newName, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(newName);

        var account = _accounts.FirstOrDefault(a => a.Id.Equals(accountId))
            ?? throw ChartOfAccountsErrors.AccountNotFound(accountId.Value);

        if (account.Name.Equals(newName))
            return; // idempotent

        var oldName = account.Name;
        account.Rename(newName, occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new AccountRenamed(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            ChartOfAccountsId: Id,
            AccountId: accountId,
            OldName: oldName.Value,
            NewName: newName.Value));
    }

    public void DeactivateAccount(AccountId accountId, DateTime occurredAt)
    {
        var account = _accounts.FirstOrDefault(a => a.Id.Equals(accountId))
            ?? throw ChartOfAccountsErrors.AccountNotFound(accountId.Value);

        if (!account.IsActive)
            throw ChartOfAccountsErrors.AccountAlreadyInactive(accountId.Value);

        if (_accounts.Any(a => a.ParentId.HasValue
                            && a.ParentId.Value.Equals(accountId)
                            && a.IsActive))
        {
            throw ChartOfAccountsErrors.CannotDeactivateAccountWithActiveChildren();
        }

        account.Deactivate(occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new AccountDeactivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            ChartOfAccountsId: Id,
            AccountId: accountId));
    }

    private int ComputeDepth(Account account)
    {
        var depth = 1;
        var current = account;
        while (current.ParentId.HasValue)
        {
            var parent = _accounts.FirstOrDefault(a => a.Id.Equals(current.ParentId.Value));
            if (parent is null)
                break; // orphan reference — defensive; uniqueness check above prevents this on the happy path.
            current = parent;
            depth++;
        }
        return depth;
    }
}
