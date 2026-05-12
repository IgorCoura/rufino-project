namespace AccountsPayable.UnitTests.CostCenters.Mothers;

using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.CostCenters.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public static class CostCenterMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));

    public static CostCenter Active(
        CostCenterId? id = null,
        TenantId? tenantId = null,
        string code = "OBRA-SYRAH",
        string name = "Obra Syrah")
        => CostCenter.Create(
            id: id ?? CostCenterId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            code: new CostCenterCode(code),
            name: new CostCenterName(name),
            occurredAt: DEFAULT_OCCURRED_AT);

    public static CostCenter Inactive()
    {
        var center = Active();
        center.Deactivate(DEFAULT_OCCURRED_AT.AddDays(1));
        return center;
    }
}
