namespace EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

/// <summary>
/// An additional recurring charge track bundled into a lease contract (condominium, property tax, insurance).
/// Each charge generates its own reciprocal commitment pair per period on activation. The Rent track is the
/// contract core (DefaultTerms + ResourceId + CounterpartyId) and is never represented as a ContractCharge.
/// <para>
/// <see cref="CollectedByCounterparty"/> resolves the mixed-billing case: <c>true</c> when the charge is billed
/// through the contract counterparty (pass-through, e.g. condominium on the landlord's boleto), <c>false</c> when
/// paid directly to <see cref="RecipientAgentId"/> (e.g. property tax to the municipality).
/// </para>
/// </summary>
public sealed class ContractCharge : ValueObject
{
    public CommitmentPurpose Purpose { get; private set; } = default!;
    public Money ExpectedAmount { get; private set; } = default!;
    public EconomicResourceId ResourceId { get; private set; }
    public EconomicAgentId RecipientAgentId { get; private set; }
    public bool CollectedByCounterparty { get; private set; }

    private ContractCharge() { }

    public ContractCharge(
        CommitmentPurpose purpose,
        Money expectedAmount,
        EconomicResourceId resourceId,
        EconomicAgentId recipientAgentId,
        bool collectedByCounterparty)
    {
        if (purpose is null)
            throw EconomicContractErrors.InvalidChargePurpose();
        if (expectedAmount is null || expectedAmount.Amount <= 0m)
            throw EconomicContractErrors.InvalidChargeAmount();
        if (resourceId.Equals(EconomicResourceId.Empty))
            throw EconomicContractErrors.InvalidChargeResource();
        if (recipientAgentId.Equals(EconomicAgentId.Empty))
            throw EconomicContractErrors.InvalidChargeRecipient();

        Purpose = purpose;
        ExpectedAmount = expectedAmount;
        ResourceId = resourceId;
        RecipientAgentId = recipientAgentId;
        CollectedByCounterparty = collectedByCounterparty;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Purpose;
        yield return ExpectedAmount;
        yield return ResourceId;
        yield return RecipientAgentId;
        yield return CollectedByCounterparty;
    }
}
