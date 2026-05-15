namespace AccountsPayable.UnitTests.InstallmentPlans.Mothers;

using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.InstallmentPlans.Enumerations;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public static class InstallmentPlanMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly DEFAULT_FIRST_DUE_DATE = new(2024, 2, 15);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));
    public static readonly SupplierId DEFAULT_SUPPLIER = SupplierId.From(new Guid("22222222-2222-2222-2222-222222222222"));

    public static InstallmentPlan Active(
        InstallmentPlanId? id = null,
        decimal totalAmount = 12_000m,
        int installmentCount = 12,
        DateOnly? firstDueDate = null,
        InstallmentFrequency? frequency = null,
        string description = "Aluguel anual",
        DateTime? occurredAt = null)
    {
        return InstallmentPlan.Create(
            id: id ?? InstallmentPlanId.New(),
            tenantId: DEFAULT_TENANT,
            supplierId: DEFAULT_SUPPLIER,
            totalAmount: new Money(totalAmount, Currency.Brl),
            installmentCount: installmentCount,
            firstDueDate: firstDueDate ?? DEFAULT_FIRST_DUE_DATE,
            frequency: frequency ?? InstallmentFrequency.Monthly,
            description: new Description(description),
            occurredAt: occurredAt ?? DEFAULT_OCCURRED_AT);
    }
}
