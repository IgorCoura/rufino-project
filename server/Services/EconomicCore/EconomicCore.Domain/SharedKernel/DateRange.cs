namespace EconomicCore.Domain.SharedKernel;

using EconomicCore.Domain.SeedWork;

public sealed class DateRange : ValueObject
{
    public DateOnly From { get; }
    public DateOnly To { get; }

    public DateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw DateRangeErrors.InvalidRange(from, to);

        From = from;
        To = to;
    }

    public int Days => To.DayNumber - From.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= From && date <= To;

    public bool Overlaps(DateRange other) => From <= other.To && other.From <= To;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return From;
        yield return To;
    }
}
