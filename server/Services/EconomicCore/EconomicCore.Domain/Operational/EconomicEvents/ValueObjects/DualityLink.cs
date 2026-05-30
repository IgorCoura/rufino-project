namespace EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

/// <summary>
/// A closed (or partially closed) duality leg: this event matched <see cref="MatchedAmount"/> against
/// <see cref="CounterpartEventId"/>. <see cref="CommitmentId"/> identifies which covering allocation this leg
/// settles — set for commitment-covered events (a bundled payment closes one leg per allocation), and null for a
/// directly-paired event (simultaneous exchange, no commitment).
/// </summary>
public sealed class DualityLink : ValueObject
{
    public EconomicEventId CounterpartEventId { get; private set; }
    public decimal MatchedAmountValue { get; private set; }
    public Currency MatchedCurrency { get; private set; } = default!;
    public CommitmentId? CommitmentId { get; private set; }

    // Money is composed from the two flat columns. The amount is kept as scalar columns (not an owned Money)
    // because this VO lives in an owned collection that is appended AFTER the event is persisted (CloseDuality);
    // EF does not track a second-level owned type added to an already-tracked principal, so a nested Money would
    // persist as NULL. Flat scalars (like Participation) track reliably.
    public Money MatchedAmount => new(MatchedAmountValue, MatchedCurrency);

    private DualityLink() { }

    public DualityLink(EconomicEventId counterpartEventId, Money matchedAmount, CommitmentId? commitmentId = null)
    {
        if (counterpartEventId.Equals(EconomicEventId.Empty))
            throw EconomicEventErrors.InvalidDualityCounterpart();
        if (matchedAmount is null || matchedAmount.Amount <= 0m)
            throw EconomicEventErrors.InvalidDualityMatchedAmount();

        CounterpartEventId = counterpartEventId;
        MatchedAmountValue = matchedAmount.Amount;
        MatchedCurrency = matchedAmount.Currency;
        CommitmentId = commitmentId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CounterpartEventId;
        yield return MatchedAmountValue;
        yield return MatchedCurrency;
        yield return CommitmentId;
    }
}
