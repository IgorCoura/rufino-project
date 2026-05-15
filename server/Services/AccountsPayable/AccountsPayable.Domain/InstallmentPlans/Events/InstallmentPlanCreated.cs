namespace AccountsPayable.Domain.InstallmentPlans.Events;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public sealed record InstallmentPlanCreated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    InstallmentPlanId InstallmentPlanId,
    SupplierId SupplierId,
    decimal TotalAmountValue,
    string TotalAmountCurrency,
    int InstallmentCount,
    DateOnly FirstDueDate,
    string Frequency,
    string Description) : IDomainEvent;
