namespace AccountsPayable.Domain.InstallmentPlans;

using AccountsPayable.Domain.SeedWork;

public readonly record struct InstallmentPlanId(Guid Value) : IEntityId<InstallmentPlanId>
{
    public static InstallmentPlanId New() => new(Guid.NewGuid());
    public static InstallmentPlanId From(Guid value) => new(value);
    public static InstallmentPlanId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
