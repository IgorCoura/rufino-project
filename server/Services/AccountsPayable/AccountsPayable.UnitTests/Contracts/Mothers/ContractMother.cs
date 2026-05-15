namespace AccountsPayable.UnitTests.Contracts.Mothers;

using AccountsPayable.Domain.Contracts;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public static class ContractMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly DEFAULT_START_DATE = new(2024, 1, 1);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));
    public static readonly SupplierId DEFAULT_SUPPLIER = SupplierId.From(new Guid("22222222-2222-2222-2222-222222222222"));

    public static Contract Draft(
        ContractId? id = null,
        decimal monthlyAmount = 5_000m,
        int paymentDay = 10,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool autoCreatePayable = false,
        string description = "Aluguel sede")
    {
        return Contract.Create(
            id: id ?? ContractId.New(),
            tenantId: DEFAULT_TENANT,
            supplierId: DEFAULT_SUPPLIER,
            description: new Description(description),
            startDate: startDate ?? DEFAULT_START_DATE,
            endDate: endDate,
            monthlyAmount: new Money(monthlyAmount, Currency.Brl),
            paymentDay: paymentDay,
            autoCreatePayable: autoCreatePayable,
            occurredAt: DEFAULT_OCCURRED_AT);
    }

    public static Contract Active(decimal monthlyAmount = 5_000m, int paymentDay = 10)
    {
        var c = Draft(monthlyAmount: monthlyAmount, paymentDay: paymentDay);
        c.Activate(DEFAULT_OCCURRED_AT.AddMinutes(1));
        return c;
    }
}
