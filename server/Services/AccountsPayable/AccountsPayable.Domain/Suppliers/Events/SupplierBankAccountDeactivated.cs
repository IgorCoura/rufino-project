namespace AccountsPayable.Domain.Suppliers.Events;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Conta bancária do fornecedor desativada (soft delete — a conta sai da coleção de ativos do
/// <see cref="Supplier"/> e entra na de inativos, preservando o histórico). Carrega o snapshot
/// completo da conta desativada para que handlers downstream (ex.: detector de Payable
/// desatualizado) consigam identificar a conta sem precisar consultar a coleção atual.
/// </summary>
public sealed record SupplierBankAccountDeactivated(
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
