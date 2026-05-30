namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractChargeTests
{
    private static Func<CommitmentId> SequentialFactory()
    {
        var counter = 0;
        return () =>
        {
            counter++;
            var bytes = new byte[16];
            BitConverter.GetBytes(counter).CopyTo(bytes, 0);
            return CommitmentId.From(new Guid(bytes));
        };
    }

    // AddCharge em contrato Draft adiciona a trilha de encargo (condomínio) à coleção.
    [Fact]
    public void AddCharge_OnDraftContract_ShouldAppendCharge()
    {
        var contract = EconomicContractMother.New().Build();

        EconomicContractMother.AddChargeFrom(contract, EconomicContractMother.CondominiumCharge(), EconomicContractMother.FixedOccurredAt);

        Assert.Single(contract.Charges);
        Assert.Same(CommitmentPurpose.Condominium, contract.Charges.Single().Purpose);
    }

    // AddCharge depois da ativação lança ECC.CTR22 (encargos só em Draft).
    [Fact]
    public void AddCharge_OnActiveContract_ShouldThrowECC_CTR22()
    {
        var contract = EconomicContractMother.New().WithTermMonths(1).Build();
        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        var ex = Assert.Throws<DomainException>(
            () => EconomicContractMother.AddChargeFrom(contract, EconomicContractMother.CondominiumCharge(), EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR22", ex.Id);
    }

    // AddCharge com purpose duplicado lança ECC.CTR23 (uma trilha por tipo de encargo).
    [Fact]
    public void AddCharge_WithDuplicatePurpose_ShouldThrowECC_CTR23()
    {
        var contract = EconomicContractMother.New().Build();
        EconomicContractMother.AddChargeFrom(contract, EconomicContractMother.CondominiumCharge(amount: 300m), EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(
            () => EconomicContractMother.AddChargeFrom(contract, EconomicContractMother.CondominiumCharge(amount: 400m), EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR23", ex.Id);
    }

    // AddCharge com purpose Rent (= PrimaryPurpose) lança ECC.CTR24 (trilha-núcleo implícita).
    [Fact]
    public void AddCharge_WithRentPurpose_ShouldThrowECC_CTR24()
    {
        var contract = EconomicContractMother.New().Build();

        var ex = Assert.Throws<DomainException>(
            () => contract.AddCharge(
                CommitmentPurpose.Rent, 1000m, Domain.SharedKernel.Currency.BRL,
                EconomicContractMother.FixedResourceId, EconomicContractMother.FixedCounterpartyId,
                collectedByCounterparty: true, EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR24", ex.Id);
    }

    // Activate com 1 encargo extra gera 2 trilhas por mês: 12×(rent out+in) + 12×(condo out+in) = 48 commitments.
    [Fact]
    public void Activate_WithOneExtraCharge_ShouldGenerateTwoTracksPerMonth()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(12)
            .WithCharge(EconomicContractMother.CondominiumCharge(amount: 300m))
            .Build();

        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        Assert.Equal(48, contract.Commitments.Count);
        Assert.Equal(24, contract.Commitments.Count(c => c.Purpose == CommitmentPurpose.Rent));
        Assert.Equal(24, contract.Commitments.Count(c => c.Purpose == CommitmentPurpose.Condominium));
    }

    // Cada trilha gerada usa o valor do seu próprio encargo (aluguel 1000, condomínio 300).
    [Fact]
    public void Activate_WithExtraCharge_ShouldUsePerTrackExpectedAmount()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(1)
            .WithCharge(EconomicContractMother.CondominiumCharge(amount: 300m))
            .Build();

        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        var rentOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Rent && c.Direction == CommitmentDirection.OutflowPromise);
        var condoOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Condominium && c.Direction == CommitmentDirection.OutflowPromise);

        Assert.Equal(1000m, rentOutflow.ExpectedAmount.Amount);
        Assert.Equal(300m, condoOutflow.ExpectedAmount.Amount);
    }

    // Encargos distintos coexistem no mesmo período sem disparar o duplicate-check CTR02.
    [Fact]
    public void Activate_WithTwoExtraCharges_ShouldGenerateThreeTracksPerMonth()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(2)
            .WithCharge(EconomicContractMother.CondominiumCharge(amount: 300m))
            .WithCharge(EconomicContractMother.PropertyTaxCharge(amount: 200m))
            .Build();

        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        Assert.Equal(12, contract.Commitments.Count);
        Assert.Equal(4, contract.Commitments.Count(c => c.Purpose == CommitmentPurpose.Rent));
        Assert.Equal(4, contract.Commitments.Count(c => c.Purpose == CommitmentPurpose.Condominium));
        Assert.Equal(4, contract.Commitments.Count(c => c.Purpose == CommitmentPurpose.PropertyTax));
    }

    // FindPromisedCommitment com purpose desambigua a trilha correta no mesmo período/direção.
    [Fact]
    public void FindPromisedCommitment_WithPurpose_ShouldReturnMatchingTrack()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(1)
            .WithCharge(EconomicContractMother.CondominiumCharge(amount: 300m))
            .Build();
        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());
        var period = EconomicContractMother.October2025();

        var condo = contract.FindPromisedCommitment(period, CommitmentDirection.OutflowPromise, CommitmentPurpose.Condominium);

        Assert.Same(CommitmentPurpose.Condominium, condo.Purpose);
        Assert.Equal(300m, condo.ExpectedAmount.Amount);
    }
}
