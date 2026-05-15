namespace AccountsPayable.UnitTests.AutoApprovalPolicies.Mothers;

using AccountsPayable.Domain.AutoApprovalPolicies;
using AccountsPayable.Domain.AutoApprovalPolicies.Entities;
using AccountsPayable.Domain.AutoApprovalPolicies.ValueObjects;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public static class AutoApprovalPolicyMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));
    public static readonly SupplierId DEFAULT_SUPPLIER = SupplierId.From(new Guid("22222222-2222-2222-2222-222222222222"));

    public static AutoApprovalPolicy Empty(TenantId? tenantId = null) =>
        AutoApprovalPolicy.Create(
            id: AutoApprovalPolicyId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            occurredAt: DEFAULT_OCCURRED_AT);

    public static ApproverRoles Partners() => new(new[] { "OWNER", "PARTNER" });

    public static ApprovalMatchCriteria SupplierCriteria(SupplierId? supplier = null) =>
        new(supplierId: supplier ?? DEFAULT_SUPPLIER);

    public static AutoApprovalPolicy WithAmountThresholdRule(
        out ApprovalRuleId ruleId,
        decimal threshold = 10_000m,
        int requiredCount = 2)
    {
        var policy = Empty();
        ruleId = ApprovalRuleId.New();
        policy.AddRule(
            ruleId: ruleId,
            matchCriteria: new ApprovalMatchCriteria(minAmount: new Money(1m, Currency.Brl)), // matches any positive amount
            thresholdAmount: new Money(threshold, Currency.Brl),
            requiredApproverRoles: Partners(),
            requiredApprovalCount: requiredCount,
            occurredAt: DEFAULT_OCCURRED_AT.AddMinutes(1));
        return policy;
    }
}
