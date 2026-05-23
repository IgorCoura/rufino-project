namespace EconomicCore.Domain.SharedKernel;

using EconomicCore.Domain.SeedWork;

public sealed class CompetencePeriod : ValueObject
{
    public const int MIN_YEAR = 1900;
    public const int MAX_YEAR = 9999;
    public const int MIN_MONTH = 1;
    public const int MAX_MONTH = 12;

    public int Year { get; }
    public int Month { get; }

    public CompetencePeriod(int year, int month)
    {
        if (year < MIN_YEAR || year > MAX_YEAR)
            throw CompetencePeriodErrors.InvalidYear(year);
        if (month < MIN_MONTH || month > MAX_MONTH)
            throw CompetencePeriodErrors.InvalidMonth(month);

        Year = year;
        Month = month;
    }

    public CompetencePeriod Next()
        => Month == MAX_MONTH
            ? new CompetencePeriod(Year + 1, MIN_MONTH)
            : new CompetencePeriod(Year, Month + 1);

    public CompetencePeriod Previous()
        => Month == MIN_MONTH
            ? new CompetencePeriod(Year - 1, MAX_MONTH)
            : new CompetencePeriod(Year, Month - 1);

    public DateOnly FirstDay() => new(Year, Month, 1);
    public DateOnly LastDay() => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    public bool Contains(DateOnly date)
        => date.Year == Year && date.Month == Month;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Year;
        yield return Month;
    }
}
