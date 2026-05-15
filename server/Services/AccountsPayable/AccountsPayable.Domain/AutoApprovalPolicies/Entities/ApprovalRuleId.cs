namespace AccountsPayable.Domain.AutoApprovalPolicies.Entities;

using AccountsPayable.Domain.SeedWork;

public readonly record struct ApprovalRuleId(Guid Value) : IEntityId<ApprovalRuleId>
{
    public static ApprovalRuleId New() => new(Guid.NewGuid());
    public static ApprovalRuleId From(Guid value) => new(value);
    public static ApprovalRuleId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
