namespace EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

/// <summary>
/// One leg of a (possibly bundled) cash event: how much of the event's total <see cref="Money"/> settles a
/// specific covering commitment. A single boleto that pays rent + condominium + property tax is one EconomicEvent
/// with three allocations — the Materialized Claim pattern (Hruby §5.6): unified on the cash side, distinct on the
/// obligation side. A simple 1:1 payment/consumption has exactly one allocation for the full event amount.
/// Immutable; the duality closure for each allocation is recorded separately in the event's DualityLinks.
/// </summary>
public sealed class PaymentAllocation : ValueObject
{
    public CommitmentRef Commitment { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;

    private PaymentAllocation() { }

    public PaymentAllocation(CommitmentRef commitment, Money amount)
    {
        if (commitment is null)
            throw EconomicEventErrors.InvalidAllocation();
        if (amount is null || amount.Amount <= 0m)
            throw EconomicEventErrors.InvalidAllocation();

        Commitment = commitment;
        Amount = amount;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Commitment;
        yield return Amount;
    }
}
