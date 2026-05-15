namespace AccountsPayable.UnitTests.ExpenseClassificationRules.Mothers;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.ExpenseClassificationRules;
using AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public static class ExpenseClassificationRuleMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));
    public static readonly SupplierId DEFAULT_SUPPLIER = SupplierId.From(new Guid("22222222-2222-2222-2222-222222222222"));
    public static readonly AccountId DEFAULT_ACCOUNT = AccountId.From(new Guid("33333333-3333-3333-3333-333333333333"));
    public static readonly CostCenterId DEFAULT_COST_CENTER = CostCenterId.From(new Guid("44444444-4444-4444-4444-444444444444"));
    public static readonly UserId DEFAULT_USER = UserId.From(new Guid("55555555-5555-5555-5555-555555555555"));

    public static ClassificationMatcher SupplierMatcher(SupplierId? supplier = null) =>
        new(supplierId: supplier ?? DEFAULT_SUPPLIER);

    public static ClassificationAction DefaultAction(bool autoApprove = false) =>
        new(DEFAULT_ACCOUNT, DEFAULT_COST_CENTER, autoApprove);

    public static ExpenseClassificationRule Active(
        ExpenseClassificationRuleId? id = null,
        ClassificationMatcher? match = null,
        ClassificationAction? action = null,
        int priority = 5,
        TenantId? tenantId = null)
    {
        return ExpenseClassificationRule.CreateManual(
            id: id ?? ExpenseClassificationRuleId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            match: match ?? SupplierMatcher(),
            action: action ?? DefaultAction(),
            priority: priority,
            occurredAt: DEFAULT_OCCURRED_AT);
    }

    public static ExpenseClassificationRule Learned(
        ExpenseClassificationRuleId? id = null,
        UserId? learnedFromUserId = null,
        int priority = 3)
    {
        return ExpenseClassificationRule.LearnFromHistory(
            id: id ?? ExpenseClassificationRuleId.New(),
            tenantId: DEFAULT_TENANT,
            match: SupplierMatcher(),
            action: DefaultAction(),
            priority: priority,
            learnedFromUserId: learnedFromUserId ?? DEFAULT_USER,
            occurredAt: DEFAULT_OCCURRED_AT);
    }
}
