namespace EconomicCore.Domain.Services;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SharedKernel;

/// <summary>
/// Coordinates the closure of the duality between two EconomicEvents covered by reciprocal Commitments. Each event
/// settles a specific covering allocation (identified by its commitment), so a bundled payment can close one leg at
/// a time against each reciprocal consumption. Stateless, no infra dependency, no async, no events emitted directly —
/// the aggregates emit their own DualityClosed.
/// </summary>
public static class DualityMatchingService
{
    public static void Match(
        EconomicEvent paymentEvent,
        CommitmentId paymentCommitmentId,
        EconomicEvent consumptionEvent,
        CommitmentId consumptionCommitmentId,
        DateTime occurredAt)
    {
        if (paymentEvent is null)
            throw DualityMatchingErrors.NullEvent(nameof(paymentEvent));
        if (consumptionEvent is null)
            throw DualityMatchingErrors.NullEvent(nameof(consumptionEvent));

        if (!paymentEvent.TenantId.Equals(consumptionEvent.TenantId))
            throw DualityMatchingErrors.TenantMismatch(paymentEvent.TenantId.Value, consumptionEvent.TenantId.Value);

        if (!paymentEvent.Allocations.Any(a => a.Commitment.CommitmentId.Equals(paymentCommitmentId)))
            throw DualityMatchingErrors.PaymentNotCoveredByCommitment();

        if (!consumptionEvent.Allocations.Any(a => a.Commitment.CommitmentId.Equals(consumptionCommitmentId)))
            throw DualityMatchingErrors.ConsumptionNotCoveredByCommitment();

        if (!paymentEvent.Amount.Currency.Equals(consumptionEvent.Amount.Currency))
            throw DualityMatchingErrors.CurrencyMismatch(
                paymentEvent.Amount.Currency.Name, consumptionEvent.Amount.Currency.Name);

        var paymentRemaining = RemainingBalance(paymentEvent, paymentCommitmentId);
        var consumptionRemaining = RemainingBalance(consumptionEvent, consumptionCommitmentId);
        var matchedValue = Math.Min(paymentRemaining, consumptionRemaining);
        var matchedAmount = new Money(matchedValue, paymentEvent.Amount.Currency);

        paymentEvent.CloseDuality(paymentCommitmentId, consumptionEvent.Id, matchedAmount, occurredAt);
        consumptionEvent.CloseDuality(consumptionCommitmentId, paymentEvent.Id, matchedAmount, occurredAt);
    }

    private static decimal RemainingBalance(EconomicEvent ev, CommitmentId commitmentId)
    {
        var allocation = ev.Allocations.First(a => a.Commitment.CommitmentId.Equals(commitmentId));
        return allocation.Amount.Amount - ev.MatchedAmountFor(commitmentId);
    }
}
