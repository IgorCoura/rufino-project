namespace EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed class DualityLink : ValueObject
{
    public EconomicEventId CounterpartEventId { get; }
    public Money MatchedAmount { get; }

    public DualityLink(EconomicEventId counterpartEventId, Money matchedAmount)
    {
        if (counterpartEventId.Equals(EconomicEventId.Empty))
            throw EconomicEventErrors.InvalidDualityCounterpart();
        if (matchedAmount is null || matchedAmount.Amount <= 0m)
            throw EconomicEventErrors.InvalidDualityMatchedAmount();

        CounterpartEventId = counterpartEventId;
        MatchedAmount = matchedAmount;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CounterpartEventId;
        yield return MatchedAmount;
    }
}
