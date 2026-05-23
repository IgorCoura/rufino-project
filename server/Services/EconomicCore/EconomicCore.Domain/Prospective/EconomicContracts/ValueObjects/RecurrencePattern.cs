namespace EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;

public sealed class RecurrencePattern : ValueObject
{
    public const int MIN_ANCHOR_DAY = 1;
    public const int MAX_ANCHOR_DAY = 31;

    public Periodicity Periodicity { get; }
    public int AnchorDay { get; }

    public RecurrencePattern(Periodicity periodicity, int anchorDay)
    {
        if (periodicity is null)
            throw EconomicContractErrors.InvalidRecurrencePeriodicity();
        if (anchorDay < MIN_ANCHOR_DAY || anchorDay > MAX_ANCHOR_DAY)
            throw EconomicContractErrors.InvalidRecurrenceAnchorDay(anchorDay);

        Periodicity = periodicity;
        AnchorDay = anchorDay;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Periodicity;
        yield return AnchorDay;
    }
}
