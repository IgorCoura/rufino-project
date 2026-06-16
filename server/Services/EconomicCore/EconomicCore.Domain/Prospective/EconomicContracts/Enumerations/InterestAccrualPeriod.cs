namespace EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;

using EconomicCore.Domain.SeedWork;

/// <summary>
/// Time unit over which late-payment interest accrues. Interest charges one rate (or fixed amount)
/// per fully elapsed unit: Daily counts exact days between due date and payment date; Monthly counts
/// calendar-month differences (paying within the due month accrues nothing); Yearly counts
/// calendar-year differences, symmetric with the monthly rule.
/// </summary>
public sealed class InterestAccrualPeriod : Enumeration
{
    public static readonly InterestAccrualPeriod Daily = new(1, "DAILY");
    public static readonly InterestAccrualPeriod Monthly = new(2, "MONTHLY");
    public static readonly InterestAccrualPeriod Yearly = new(3, "YEARLY");

    private InterestAccrualPeriod(int id, string name) : base(id, name) { }

    /// <summary>Fully elapsed units between <paramref name="dueDate"/> and <paramref name="paidDate"/> (never negative).</summary>
    public int ElapsedUnits(DateOnly dueDate, DateOnly paidDate)
    {
        if (paidDate <= dueDate)
            return 0;

        if (this == Daily)
            return paidDate.DayNumber - dueDate.DayNumber;

        if (this == Monthly)
            return ((paidDate.Year * 12) + paidDate.Month) - ((dueDate.Year * 12) + dueDate.Month);

        return paidDate.Year - dueDate.Year;
    }
}
