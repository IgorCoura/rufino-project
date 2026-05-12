namespace AccountsPayable.Domain.Suppliers;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Entities;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.Events;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// Aggregate Root for a supplier (fornecedor). Traditional snapshot persistence (EF Core),
/// not Event-Sourced. Uniqueness of <see cref="TaxId"/> per tenant is enforced by the
/// <c>SupplierUniquenessChecker</c> Domain Service (cross-aggregate check via port), since
/// the Aggregate cannot see other Suppliers in the same tenant.
/// </summary>
public sealed class Supplier : AggregateRoot<SupplierId>
{
    private readonly List<SupplierBankAccount> _bankAccounts = [];

    public TenantId TenantId { get; private set; }
    public LegalName LegalName { get; private set; } = default!;
    public TradeName? TradeName { get; private set; }
    public TaxId TaxId { get; private set; } = default!;
    public ContactInfo Contact { get; private set; } = default!;
    public SupplierStatus Status { get; private set; } = default!;

    public IReadOnlyCollection<SupplierBankAccount> BankAccounts => _bankAccounts.AsReadOnly();

    private Supplier() : base() { }

    private Supplier(SupplierId id) : base(id) { }

    public static Supplier Create(
        SupplierId id,
        TenantId tenantId,
        LegalName legalName,
        TradeName? tradeName,
        TaxId taxId,
        ContactInfo contact,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(legalName);
        ArgumentNullException.ThrowIfNull(taxId);
        ArgumentNullException.ThrowIfNull(contact);

        var supplier = new Supplier(id)
        {
            TenantId = tenantId,
            LegalName = legalName,
            TradeName = tradeName,
            TaxId = taxId,
            Contact = contact,
            Status = SupplierStatus.Active,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        supplier.AddDomainEvent(new SupplierCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: tenantId,
            SupplierId: id,
            LegalName: legalName.Value,
            TradeName: tradeName?.Value,
            TaxIdValue: taxId.Value,
            TaxIdType: taxId.Type.Name));

        return supplier;
    }

    public void Rename(LegalName newName, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(newName);
        if (newName.Equals(LegalName))
            return; // idempotent: no event for no-op

        var oldName = LegalName;
        LegalName = newName;
        UpdatedAt = occurredAt;

        AddDomainEvent(new SupplierRenamed(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id,
            OldLegalName: oldName.Value,
            NewLegalName: newName.Value));
    }

    public void UpdateContact(ContactInfo newContact, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(newContact);
        if (newContact.Equals(Contact))
            return;

        Contact = newContact;
        UpdatedAt = occurredAt;

        AddDomainEvent(new SupplierContactUpdated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id,
            Email: newContact.Email.Value,
            Phone: newContact.Phone?.Value));
    }

    public SupplierBankAccount AddBankAccount(
        SupplierBankAccountId bankAccountId,
        string bankCode,
        string branch,
        string accountNumber,
        BankAccountType accountType,
        PixKey? pixKey,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(accountType);

        if (_bankAccounts.Any(a =>
                a.BankCode == NormalizeBankCode(bankCode)
                && a.Branch == (branch?.Trim() ?? string.Empty)
                && a.AccountNumber == (accountNumber?.Trim() ?? string.Empty)))
        {
            throw SupplierErrors.DuplicatedBankAccount(bankCode, branch ?? string.Empty, accountNumber ?? string.Empty);
        }

        var account = new SupplierBankAccount(
            bankAccountId, bankCode, branch!, accountNumber!, accountType, pixKey, occurredAt);

        _bankAccounts.Add(account);
        UpdatedAt = occurredAt;

        AddDomainEvent(new SupplierBankAccountAdded(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id,
            BankAccountId: bankAccountId,
            BankCode: account.BankCode,
            Branch: account.Branch,
            AccountNumber: account.AccountNumber,
            AccountType: accountType.Name));

        return account;
    }

    public void RemoveBankAccount(SupplierBankAccountId bankAccountId, DateTime occurredAt)
    {
        var account = _bankAccounts.FirstOrDefault(a => a.Id.Equals(bankAccountId))
            ?? throw SupplierErrors.BankAccountNotFound(bankAccountId.Value);

        if (Status == SupplierStatus.Active && _bankAccounts.Count == 1)
            throw SupplierErrors.CannotRemoveLastBankAccountWhileActive();

        _bankAccounts.Remove(account);
        UpdatedAt = occurredAt;

        AddDomainEvent(new SupplierBankAccountRemoved(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id,
            BankAccountId: bankAccountId));
    }

    public void Block(string reason, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw SupplierErrors.ReasonRequired();
        if (!Status.CanTransitionTo(SupplierStatus.Blocked))
            throw SupplierErrors.InvalidStatusTransition(Status.Name, SupplierStatus.Blocked.Name);

        Status = SupplierStatus.Blocked;
        UpdatedAt = occurredAt;

        AddDomainEvent(new SupplierBlocked(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id,
            Reason: reason.Trim()));
    }

    public void Unblock(DateTime occurredAt)
    {
        if (Status != SupplierStatus.Blocked)
            throw SupplierErrors.InvalidStatusTransition(Status.Name, SupplierStatus.Active.Name);

        Status = SupplierStatus.Active;
        UpdatedAt = occurredAt;

        AddDomainEvent(new SupplierUnblocked(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id));
    }

    public void Deactivate(DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(SupplierStatus.Inactive))
            throw SupplierErrors.InvalidStatusTransition(Status.Name, SupplierStatus.Inactive.Name);

        Status = SupplierStatus.Inactive;
        UpdatedAt = occurredAt;

        AddDomainEvent(new SupplierDeactivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id));
    }

    public void Reactivate(DateTime occurredAt)
    {
        if (Status != SupplierStatus.Inactive)
            throw SupplierErrors.InvalidStatusTransition(Status.Name, SupplierStatus.Active.Name);

        Status = SupplierStatus.Active;
        UpdatedAt = occurredAt;

        AddDomainEvent(new SupplierReactivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id));
    }

    private static string NormalizeBankCode(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? string.Empty
            : new string(raw.Where(char.IsDigit).ToArray());
}
