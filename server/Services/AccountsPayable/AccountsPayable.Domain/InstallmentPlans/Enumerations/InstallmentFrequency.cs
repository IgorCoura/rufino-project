namespace AccountsPayable.Domain.InstallmentPlans.Enumerations;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// How dueDates are spaced inside an <see cref="InstallmentPlan"/>. Used by
/// <c>InstallmentPlanFactory</c> when generating the chain of <c>Payable</c>s.
/// </summary>
public sealed class InstallmentFrequency : Enumeration
{
    public static readonly InstallmentFrequency Monthly = new(1, "MONTHLY");
    public static readonly InstallmentFrequency Weekly = new(2, "WEEKLY");

    private InstallmentFrequency(int id, string name) : base(id, name) { }

    /// <summary>Returns the <paramref name="installmentIndex"/>-th due date (0-based) starting at <paramref name="firstDueDate"/>.</summary>
    public DateOnly DueDateFor(DateOnly firstDueDate, int installmentIndex) =>
        this == Monthly
            ? firstDueDate.AddMonths(installmentIndex)
            : firstDueDate.AddDays(installmentIndex * 7);
}
