namespace AccountsPayable.Domain.Suppliers;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.Events;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// Aggregate Root for a supplier (fornecedor). Traditional snapshot persistence (EF Core),
/// not Event-Sourced. Uniqueness of <see cref="TaxId"/> per tenant is enforced by the
/// <c>SupplierUniquenessChecker</c> Domain Service (cross-aggregate check via port), since
/// the Aggregate cannot see other Suppliers in the same tenant.
/// <para>
/// Bank accounts are <see cref="SupplierBankAccount"/> Value Objects (sealed hierarchy:
/// <see cref="SupplierPixAccount"/> | <see cref="SupplierBankTransferAccount"/>). Soft delete via
/// two parallel collections — <see cref="ActiveBankAccounts"/> and <see cref="InactiveBankAccounts"/>.
/// The active collection is duplicate-free (invariant); the inactive collection may carry the same
/// VO more than once when an account is added → deactivated → re-added → deactivated, preserving
/// the full history snapshot. Per-event audit lives in the domain event stream.
/// </para>
/// </summary>
public sealed class Supplier : AggregateRoot<SupplierId>
{
    private readonly List<SupplierBankAccount> _activeBankAccounts = [];
    private readonly List<SupplierBankAccount> _inactiveBankAccounts = [];

    public TenantId TenantId { get; private set; }
    public LegalName LegalName { get; private set; } = default!;
    public TradeName? TradeName { get; private set; }
    public TaxId TaxId { get; private set; } = default!;
    public ContactInfo Contact { get; private set; } = default!;
    public SupplierStatus Status { get; private set; } = default!;

    public IReadOnlyCollection<SupplierBankAccount> ActiveBankAccounts => _activeBankAccounts.AsReadOnly();
    public IReadOnlyCollection<SupplierBankAccount> InactiveBankAccounts => _inactiveBankAccounts.AsReadOnly();

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
            return;

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

    public void AddBankAccount(SupplierBankAccount account, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (_activeBankAccounts.Contains(account))
            throw SupplierErrors.DuplicatedBankAccount(DescribeAccount(account));

        _activeBankAccounts.Add(account);
        UpdatedAt = occurredAt;

        var (variant, pixVal, pixType, bankCode, branch, accNumber, accType) = ExpandAccount(account);
        AddDomainEvent(new SupplierBankAccountAdded(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id,
            Variant: variant,
            PixKeyValue: pixVal,
            PixKeyType: pixType,
            BankCode: bankCode,
            Branch: branch,
            AccountNumber: accNumber,
            AccountType: accType));
    }

    public void DeactivateBankAccount(SupplierBankAccount account, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!_activeBankAccounts.Contains(account))
            throw SupplierErrors.BankAccountNotFound();

        if (Status == SupplierStatus.Active && _activeBankAccounts.Count == 1)
            throw SupplierErrors.CannotRemoveLastBankAccountWhileActive();

        _activeBankAccounts.Remove(account);
        _inactiveBankAccounts.Add(account);
        UpdatedAt = occurredAt;

        var (variant, pixVal, pixType, bankCode, branch, accNumber, accType) = ExpandAccount(account);
        AddDomainEvent(new SupplierBankAccountDeactivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            SupplierId: Id,
            Variant: variant,
            PixKeyValue: pixVal,
            PixKeyType: pixType,
            BankCode: bankCode,
            Branch: branch,
            AccountNumber: accNumber,
            AccountType: accType));
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

    private static (string Variant, string? PixKeyValue, string? PixKeyType,
                    string? BankCode, string? Branch, string? AccountNumber, string? AccountType)
        ExpandAccount(SupplierBankAccount account)
        => account switch
        {
            SupplierPixAccount p
                => ("PIX", p.PixKey.Value, p.PixKey.Type.Name, null, null, null, null),
            SupplierBankTransferAccount b
                => ("BANK_TRANSFER", null, null, b.BankCode, b.Branch, b.AccountNumber, b.AccountType.Name),
            _ => throw new InvalidOperationException(
                $"Variant desconhecida de SupplierBankAccount: {account.GetType().Name}."),
        };

    private static string DescribeAccount(SupplierBankAccount account)
        => account switch
        {
            SupplierPixAccount p => $"PIX {p.PixKey.Type.Name}",
            SupplierBankTransferAccount b => $"banco {b.BankCode} ag.{b.Branch} c/c {b.AccountNumber}",
            _ => account.GetType().Name,
        };
}
