namespace AccountsPayable.Domain.AutoApprovalPolicies;

using AccountsPayable.Domain.SeedWork;

public readonly record struct AutoApprovalPolicyId(Guid Value) : IEntityId<AutoApprovalPolicyId>
{
    public static AutoApprovalPolicyId New() => new(Guid.NewGuid());
    public static AutoApprovalPolicyId From(Guid value) => new(value);
    public static AutoApprovalPolicyId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
