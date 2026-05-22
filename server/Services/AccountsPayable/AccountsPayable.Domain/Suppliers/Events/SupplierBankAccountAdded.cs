namespace AccountsPayable.Domain.Suppliers.Events;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Conta bancária adicionada à coleção de ativos do <see cref="Supplier"/>. Carrega o snapshot
/// completo da conta — variante (PIX ou transferência bancária) e os dados próprios de cada
/// variante. Os campos da variante não usada permanecem null.
/// </summary>
public sealed record SupplierBankAccountAdded(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    SupplierId SupplierId,
    string Variant,
    string? PixKeyValue,
    string? PixKeyType,
    string? BankCode,
    string? Branch,
    string? AccountNumber,
    string? AccountType) : IDomainEvent;
