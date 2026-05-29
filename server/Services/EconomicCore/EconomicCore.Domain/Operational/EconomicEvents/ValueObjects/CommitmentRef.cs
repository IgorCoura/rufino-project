namespace EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;

/// <summary>
/// Cross-aggregate reference to a Commitment. Anchored on the owning aggregate root
/// (<see cref="EconomicContractId"/>) plus the internal entity id (<see cref="CommitmentId"/>),
/// so a covered EconomicEvent never points at the Contract's internal entity in isolation
/// (aggregate boundary rule: references cross aggregates by root, not by inner entity).
/// </summary>
public sealed class CommitmentRef : ValueObject
{
    public EconomicContractId ContractId { get; }
    public CommitmentId CommitmentId { get; }

    public CommitmentRef(EconomicContractId contractId, CommitmentId commitmentId)
    {
        if (contractId.Equals(EconomicContractId.Empty) || commitmentId.Equals(CommitmentId.Empty))
            throw EconomicEventErrors.InvalidCommitmentRef();

        ContractId = contractId;
        CommitmentId = commitmentId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ContractId;
        yield return CommitmentId;
    }
}
