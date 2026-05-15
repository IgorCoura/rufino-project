namespace AccountsPayable.Domain.InstallmentPlans.Events;

using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.SeedWork;

public sealed record PayableLinkedToInstallmentPlan(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    InstallmentPlanId InstallmentPlanId,
    PayableId PayableId,
    int InstallmentNumber) : IDomainEvent;
