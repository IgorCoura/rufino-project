namespace EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;

public sealed class CommitmentRef : ValueObject
{
    public CommitmentId CommitmentId { get; }

    public CommitmentRef(CommitmentId commitmentId)
    {
        if (commitmentId.Equals(CommitmentId.Empty))
            throw EconomicEventErrors.InvalidCommitmentRef();

        CommitmentId = commitmentId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CommitmentId;
    }
}
