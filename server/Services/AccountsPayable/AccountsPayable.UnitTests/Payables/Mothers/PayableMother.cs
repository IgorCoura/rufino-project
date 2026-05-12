namespace AccountsPayable.UnitTests.Payables.Mothers;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public static class PayableMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly DEFAULT_DUE_DATE = new(2024, 2, 15);
    public static readonly DateOnly DEFAULT_SCHEDULED_FOR = new(2024, 2, 14);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));
    public static readonly SupplierId DEFAULT_SUPPLIER = SupplierId.From(new Guid("22222222-2222-2222-2222-222222222222"));
    public static readonly AccountId DEFAULT_ACCOUNT = AccountId.From(new Guid("33333333-3333-3333-3333-333333333333"));
    public static readonly CostCenterId DEFAULT_COST_CENTER = CostCenterId.From(new Guid("44444444-4444-4444-4444-444444444444"));
    public static readonly UserId DEFAULT_USER = UserId.From(new Guid("55555555-5555-5555-5555-555555555555"));

    public static readonly PaymentProof DEFAULT_PROOF =
        new("https://docs.acme.com.br/payable/proof.pdf", PaymentProofType.Receipt);

    public static Payable Draft(
        PayableId? id = null,
        TenantId? tenantId = null,
        SupplierId? supplierId = null,
        decimal amount = 1_500m,
        Currency? currency = null,
        DateOnly? dueDate = null,
        string description = "Aluguel sede março",
        DateTime? occurredAt = null)
    {
        return Payable.Initialize(
            id: id ?? PayableId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            supplierId: supplierId ?? DEFAULT_SUPPLIER,
            amount: new Money(amount, currency ?? Currency.Brl),
            dueDate: new DueDate(dueDate ?? DEFAULT_DUE_DATE),
            description: new Description(description),
            occurredAt: occurredAt ?? DEFAULT_OCCURRED_AT);
    }

    /// <summary>Draft already classified — ready to Schedule under the default strict invariant.</summary>
    public static Payable Classified(
        AccountId? accountId = null,
        CostCenterId? costCenterId = null,
        UserId? classifiedBy = null)
    {
        var payable = Draft();
        payable.Classify(
            accountId ?? DEFAULT_ACCOUNT,
            costCenterId ?? DEFAULT_COST_CENTER,
            classifiedBy ?? DEFAULT_USER,
            DEFAULT_OCCURRED_AT.AddMinutes(2));
        return payable;
    }

    public static Payable Scheduled(DateOnly? scheduledFor = null)
    {
        var payable = Classified();
        payable.Schedule(
            scheduledFor: scheduledFor ?? DEFAULT_SCHEDULED_FOR,
            occurredAt: DEFAULT_OCCURRED_AT.AddMinutes(5));
        return payable;
    }

    public static Payable Paid()
    {
        var payable = Scheduled();
        payable.MarkAsPaidManually(
            proof: DEFAULT_PROOF,
            paidAt: DEFAULT_OCCURRED_AT.AddDays(30),
            occurredAt: DEFAULT_OCCURRED_AT.AddDays(30).AddMinutes(1));
        return payable;
    }

    public static Payable Cancelled(string reason = "Boleto duplicado")
    {
        var payable = Draft();
        payable.Cancel(reason, DEFAULT_OCCURRED_AT.AddMinutes(5));
        return payable;
    }
}
