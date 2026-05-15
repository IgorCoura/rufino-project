namespace AccountsPayable.Domain.InstallmentPlans.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class InstallmentPlanStatus : Enumeration
{
    public static readonly InstallmentPlanStatus Active = new(1, "ACTIVE");
    public static readonly InstallmentPlanStatus Cancelled = new(2, "CANCELLED");

    private InstallmentPlanStatus(int id, string name) : base(id, name) { }
}
