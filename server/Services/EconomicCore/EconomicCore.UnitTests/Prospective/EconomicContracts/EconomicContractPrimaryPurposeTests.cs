namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractPrimaryPurposeTests
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

    private static EconomicContract InsuranceContract() => EconomicContract.Create(
        EconomicContractId.New(),
        EconomicContractMother.FixedTenantId,
        EconomicAgentId.From(new Guid("a5a5a5a5-a5a5-7a5a-8a5a-a5a5a5a5a5a5")),
        EconomicResourceId.From(new Guid("b5b5b5b5-b5b5-7b5b-8b5b-b5b5b5b5b5b5")),
        ContractDirection.Acquisition,
        Periodicity.Monthly,
        anchorDay: 10,
        expectedAmountValue: 150m,
        Currency.BRL,
        termMonths: 6,
        startDate: new DateOnly(2025, 10, 1),
        occurredAt: EconomicContractMother.FixedOccurredAt,
        primaryPurpose: CommitmentPurpose.Insurance);

    // Contrato com PrimaryPurpose=Insurance gera a trilha-núcleo marcada como INSURANCE (seguro como contrato separado).
    [Fact]
    public void Activate_InsuranceContract_ShouldTagCoreTrackAsInsurance()
    {
        var contract = InsuranceContract();

        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        Assert.Same(CommitmentPurpose.Insurance, contract.PrimaryPurpose);
        Assert.Equal(12, contract.Commitments.Count);
        Assert.All(contract.Commitments, c => Assert.Same(CommitmentPurpose.Insurance, c.Purpose));
    }

    // Num contrato de seguro, o purpose-núcleo (Insurance) não pode ser adicionado como encargo extra (ECC.CTR24).
    [Fact]
    public void AddCharge_WithPrimaryPurpose_ShouldThrowECC_CTR24()
    {
        var contract = InsuranceContract();

        var ex = Assert.Throws<DomainException>(() => contract.AddCharge(
            CommitmentPurpose.Insurance, 150m, Currency.BRL,
            EconomicResourceId.From(new Guid("b5b5b5b5-b5b5-7b5b-8b5b-b5b5b5b5b5b5")),
            EconomicAgentId.From(new Guid("a5a5a5a5-a5a5-7a5a-8a5a-a5a5a5a5a5a5")),
            collectedByCounterparty: false, EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR24", ex.Id);
    }

    // Contrato de locação (default) continua com PrimaryPurpose=Rent: nenhuma regressão.
    [Fact]
    public void Create_DefaultContract_ShouldHaveRentPrimaryPurpose()
    {
        var contract = EconomicContractMother.New().Build();

        Assert.Same(CommitmentPurpose.Rent, contract.PrimaryPurpose);
    }
}
