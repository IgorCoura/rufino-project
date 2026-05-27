namespace EconomicCore.Domain.Services;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.SharedKernel;

/// <summary>
/// Coordinates the closure of the duality between two EconomicEvents covered by reciprocal Commitments.
/// Stateless, no infra dependency, no async, no events emitted directly — the aggregates emit their own DualityClosed.
/// </summary>
public static class DualityMatchingService
{
    public static void Match(
        EconomicEvent paymentEvent,
        EconomicEvent consumptionEvent,
        DateTime occurredAt)
    {
        if (paymentEvent is null)
            throw DualityMatchingErrors.NullEvent(nameof(paymentEvent));
        if (consumptionEvent is null)
            throw DualityMatchingErrors.NullEvent(nameof(consumptionEvent));

        if (!paymentEvent.TenantId.Equals(consumptionEvent.TenantId))
            throw DualityMatchingErrors.TenantMismatch(paymentEvent.TenantId.Value, consumptionEvent.TenantId.Value);

        if (paymentEvent.CoveringCommitment is null)
            throw DualityMatchingErrors.PaymentNotCoveredByCommitment();

        if (consumptionEvent.CoveringCommitment is null)
            throw DualityMatchingErrors.ConsumptionNotCoveredByCommitment();

        if (!paymentEvent.Amount.Currency.Equals(consumptionEvent.Amount.Currency))
            throw DualityMatchingErrors.CurrencyMismatch(
                paymentEvent.Amount.Currency.Name, consumptionEvent.Amount.Currency.Name);

        var paymentRemaining = RemainingBalance(paymentEvent);
        var consumptionRemaining = RemainingBalance(consumptionEvent);
        var matchedValue = Math.Min(paymentRemaining, consumptionRemaining);
        var matchedAmount = new Money(matchedValue, paymentEvent.Amount.Currency);

        paymentEvent.CloseDuality(consumptionEvent.Id, matchedAmount, occurredAt);
        consumptionEvent.CloseDuality(paymentEvent.Id, matchedAmount, occurredAt);
    }

    private static decimal RemainingBalance(EconomicEvent ev)
    {
        var alreadyMatched = ev.Duality?.MatchedAmount.Amount ?? 0m;
        return ev.Amount.Amount - alreadyMatched;
    }
}
