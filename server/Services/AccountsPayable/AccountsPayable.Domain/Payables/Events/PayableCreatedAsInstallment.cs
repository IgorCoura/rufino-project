namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Payable nascido como parcela de um <c>InstallmentPlan</c>. A partir da Sprint 12.B carrega
/// também o método+instrumento dessa parcela específica — cada parcela tem o seu (típico de
/// 1 NF + N boletos onde cada boleto vem com seu código de barras).
/// </summary>
public sealed record PayableCreatedAsInstallment(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    TenantId TenantId,
    InstallmentPlanId InstallmentPlanId,
    int InstallmentNumber,
    SupplierId SupplierId,
    decimal AmountValue,
    string AmountCurrency,
    DateOnly DueDate,
    string Description,
    string InstrumentKind,
    string? SupplierLegalName,
    string? SupplierTaxIdValue,
    string? SupplierTaxIdType,
    string? PixKeyValue,
    string? PixKeyType,
    string? BankCode,
    string? Branch,
    string? AccountNumber,
    string? AccountType,
    string? EmvPayload,
    string? BarcodeDigits) : IDomainEvent;
