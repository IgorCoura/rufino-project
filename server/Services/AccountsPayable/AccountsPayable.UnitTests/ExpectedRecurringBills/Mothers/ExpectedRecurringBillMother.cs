namespace AccountsPayable.UnitTests.ExpectedRecurringBills.Mothers;

using AccountsPayable.Domain.Contracts;
using AccountsPayable.Domain.ExpectedRecurringBills;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public static class ExpectedRecurringBillMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly DEFAULT_DUE_DATE = new(2024, 2, 10);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));
    public static readonly SupplierId DEFAULT_SUPPLIER = SupplierId.From(new Guid("22222222-2222-2222-2222-222222222222"));
    public static readonly ContractId DEFAULT_CONTRACT = ContractId.From(new Guid("33333333-3333-3333-3333-333333333333"));

    public static ExpectedRecurringBill Pending(
        ExpectedRecurringBillId? id = null,
        decimal expectedAmount = 5_000m,
        DateOnly? dueDate = null,
        SupplierId? supplierId = null,
        TenantId? tenantId = null) =>
        ExpectedRecurringBill.ForContract(
            id: id ?? ExpectedRecurringBillId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            contractId: DEFAULT_CONTRACT,
            supplierId: supplierId ?? DEFAULT_SUPPLIER,
            expectedDueDate: dueDate ?? DEFAULT_DUE_DATE,
            expectedAmount: new Money(expectedAmount, Currency.Brl),
            occurredAt: DEFAULT_OCCURRED_AT);
}
