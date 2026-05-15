namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Variant of <see cref="PayableCreated"/> emitted when the <see cref="Payable"/> represents a
/// single installment of an <see cref="InstallmentPlan"/>. Carries the same payload plus the link
/// back to the plan (<see cref="InstallmentPlanId"/>) and the 1-based position
/// (<see cref="InstallmentNumber"/>).
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
    string Description) : IDomainEvent;
