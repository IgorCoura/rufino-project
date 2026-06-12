namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractCommitmentQueriesTests
{
    // GetInflowCommitmentIds retorna apenas as pernas inflow — o caller não raciocina sobre direção de commitment.
    [Fact]
    public void GetInflowCommitmentIds_WithGeneratedPairs_ShouldReturnOnlyInflowIds()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.GenerateCommitmentsFor(EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);
        contract.GenerateCommitmentsFor(EconomicContractMother.November2025(),
            EconomicContractMother.OutflowCommitmentIdSlot2, EconomicContractMother.InflowCommitmentIdSlot2,
            EconomicContractMother.FixedOccurredAt);

        var inflowIds = contract.GetInflowCommitmentIds();

        Assert.Equal(2, inflowIds.Count);
        Assert.Contains(EconomicContractMother.InflowCommitmentIdSlot1, inflowIds);
        Assert.Contains(EconomicContractMother.InflowCommitmentIdSlot2, inflowIds);
        Assert.DoesNotContain(EconomicContractMother.OutflowCommitmentIdSlot1, inflowIds);
        Assert.DoesNotContain(EconomicContractMother.OutflowCommitmentIdSlot2, inflowIds);
    }

    // Contrato sem commitments materializados retorna lista vazia de ids inflow.
    [Fact]
    public void GetInflowCommitmentIds_WithoutCommitments_ShouldReturnEmpty()
    {
        var contract = EconomicContractMother.New().Build();

        Assert.Empty(contract.GetInflowCommitmentIds());
    }

    // ResolveBundledCompetence devolve o período mais antigo entre os commitments cobertos, independente da ordem do input (política do agregado).
    [Fact]
    public void ResolveBundledCompetence_WithLegsAcrossMonths_ShouldReturnEarliestPeriod()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.GenerateCommitmentsFor(EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);
        contract.GenerateCommitmentsFor(EconomicContractMother.November2025(),
            EconomicContractMother.OutflowCommitmentIdSlot2, EconomicContractMother.InflowCommitmentIdSlot2,
            EconomicContractMother.FixedOccurredAt);

        var competence = contract.ResolveBundledCompetence(
            [EconomicContractMother.OutflowCommitmentIdSlot2, EconomicContractMother.OutflowCommitmentIdSlot1]);

        Assert.Equal(2025, competence.Year);
        Assert.Equal(10, competence.Month);
    }

    // ResolveBundledCompetence sem commitments informados lança ECC.EVT17 (alocações vazias).
    [Fact]
    public void ResolveBundledCompetence_WithEmptyList_ShouldThrowECC_EVT17()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();

        var ex = Assert.Throws<DomainException>(() => contract.ResolveBundledCompetence([]));

        Assert.Equal("ECC.EVT17", ex.Id);
    }

    // ResolveBundledCompetence com commitment que não pertence ao contrato lança ECC.CTR12.
    [Fact]
    public void ResolveBundledCompetence_WithUnknownCommitmentId_ShouldThrowECC_CTR12()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();

        var ex = Assert.Throws<DomainException>(() => contract.ResolveBundledCompetence(
            [CommitmentId.From(new Guid("0e0e0e0e-0e0e-7e0e-8e0e-0e0e0e0e0e0e"))]));

        Assert.Equal("ECC.CTR12", ex.Id);
    }
}
