namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;

public class CommitmentRefTests
{
    private static readonly EconomicContractId ValidContractId = EconomicContractId.From(new Guid("eeeeeeee-eeee-7eee-8eee-eeeeeeeeeeee"));
    private static readonly CommitmentId ValidCommitmentId = CommitmentId.From(new Guid("cccccccc-cccc-7ccc-8ccc-cccccccccccc"));

    // Construção válida preserva ContractId e CommitmentId (ref ancorada na raiz do aggregate dono).
    [Fact]
    public void Constructor_WithValidIds_ShouldStoreValues()
    {
        var commitmentRef = new CommitmentRef(ValidContractId, ValidCommitmentId);

        Assert.Equal(ValidContractId, commitmentRef.ContractId);
        Assert.Equal(ValidCommitmentId, commitmentRef.CommitmentId);
    }

    // CommitmentId vazio lança ECC.EVT13.
    [Fact]
    public void Constructor_WithEmptyCommitmentId_ShouldThrowECC_EVT13()
    {
        var ex = Assert.Throws<DomainException>(() => new CommitmentRef(ValidContractId, CommitmentId.Empty));

        Assert.Equal("ECC.EVT13", ex.Id);
    }

    // Regressão: a ref cross-aggregate precisa ancorar a raiz — ContractId vazio também lança ECC.EVT13
    // (antes o CommitmentRef carregava só o CommitmentId, referenciando uma Entity interna do contrato sem a raiz).
    [Fact]
    public void Constructor_WithEmptyContractId_ShouldThrowECC_EVT13()
    {
        var ex = Assert.Throws<DomainException>(() => new CommitmentRef(EconomicContractId.Empty, ValidCommitmentId));

        Assert.Equal("ECC.EVT13", ex.Id);
    }

    // Dois CommitmentRefs com mesmos ContractId e CommitmentId são iguais (igualdade por valor).
    [Fact]
    public void Equals_SameIds_ShouldBeTrue()
    {
        var a = new CommitmentRef(ValidContractId, ValidCommitmentId);
        var b = new CommitmentRef(ValidContractId, ValidCommitmentId);

        Assert.Equal(a, b);
    }

    // Mesmo CommitmentId mas ContractId diferente quebra a igualdade — a raiz faz parte da identidade da ref.
    [Fact]
    public void Equals_DifferentContractId_ShouldBeFalse()
    {
        var other = EconomicContractId.From(new Guid("ffffffff-ffff-7fff-8fff-ffffffffffff"));

        var a = new CommitmentRef(ValidContractId, ValidCommitmentId);
        var b = new CommitmentRef(other, ValidCommitmentId);

        Assert.NotEqual(a, b);
    }
}
