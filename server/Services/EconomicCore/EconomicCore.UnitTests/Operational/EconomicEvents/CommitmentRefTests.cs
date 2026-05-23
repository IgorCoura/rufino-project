namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;

public class CommitmentRefTests
{
    private static readonly CommitmentId ValidCommitmentId = CommitmentId.From(new Guid("cccccccc-cccc-7ccc-8ccc-cccccccccccc"));

    // Construção válida preserva o CommitmentId.
    [Fact]
    public void Constructor_WithValidCommitmentId_ShouldStoreValue()
    {
        var commitmentRef = new CommitmentRef(ValidCommitmentId);

        Assert.Equal(ValidCommitmentId, commitmentRef.CommitmentId);
    }

    // CommitmentId vazio lança ECC.EVT13.
    [Fact]
    public void Constructor_WithEmptyCommitmentId_ShouldThrowECC_EVT13()
    {
        var ex = Assert.Throws<DomainException>(() => new CommitmentRef(CommitmentId.Empty));

        Assert.Equal("ECC.EVT13", ex.Id);
    }

    // Dois CommitmentRefs com mesmo Id são iguais.
    [Fact]
    public void Equals_SameCommitmentId_ShouldBeTrue()
    {
        var a = new CommitmentRef(ValidCommitmentId);
        var b = new CommitmentRef(ValidCommitmentId);

        Assert.Equal(a, b);
    }
}
