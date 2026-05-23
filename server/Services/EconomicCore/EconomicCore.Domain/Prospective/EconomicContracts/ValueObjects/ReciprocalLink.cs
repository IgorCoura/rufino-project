namespace EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;

using EconomicCore.Domain.SeedWork;

public sealed class ReciprocalLink : ValueObject
{
    public CommitmentId ReciprocalCommitmentId { get; }

    public ReciprocalLink(CommitmentId reciprocalCommitmentId)
    {
        if (reciprocalCommitmentId.Equals(CommitmentId.Empty))
            throw EconomicContractErrors.InvalidReciprocalLink();

        ReciprocalCommitmentId = reciprocalCommitmentId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ReciprocalCommitmentId;
    }
}
