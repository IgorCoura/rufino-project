namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class ContractChargeTests
{
    private static readonly EconomicResourceId ResourceId = EconomicResourceId.From(new Guid("dddddddd-dddd-7ddd-8ddd-dddddddddddd"));
    private static readonly EconomicAgentId RecipientId = EconomicAgentId.From(new Guid("ffffffff-ffff-7fff-8fff-ffffffffffff"));

    private static Money ValidAmount() => new(300m, Currency.BRL);

    // ContractCharge válido preserva purpose, amount, resource, recipient e flag de cobrança pass-through.
    [Fact]
    public void Constructor_WithValidArguments_ShouldExposeAllComponents()
    {
        var charge = new ContractCharge(CommitmentPurpose.Condominium, ValidAmount(), ResourceId, RecipientId, collectedByCounterparty: true);

        Assert.Same(CommitmentPurpose.Condominium, charge.Purpose);
        Assert.Equal(ValidAmount(), charge.ExpectedAmount);
        Assert.Equal(ResourceId, charge.ResourceId);
        Assert.Equal(RecipientId, charge.RecipientAgentId);
        Assert.True(charge.CollectedByCounterparty);
    }

    // Purpose nulo é rejeitado com ECC.CTR28.
    [Fact]
    public void Constructor_WithNullPurpose_ShouldThrowECC_CTR28()
    {
        var ex = Assert.Throws<DomainException>(
            () => new ContractCharge(null!, ValidAmount(), ResourceId, RecipientId, collectedByCounterparty: true));

        Assert.Equal("ECC.CTR28", ex.Id);
    }

    // Amount zero ou negativo é rejeitado com ECC.CTR25 (encargo precisa de valor positivo).
    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Constructor_WithNonPositiveAmount_ShouldThrowECC_CTR25(decimal amount)
    {
        var ex = Assert.Throws<DomainException>(
            () => new ContractCharge(CommitmentPurpose.Condominium, new Money(amount, Currency.BRL), ResourceId, RecipientId, collectedByCounterparty: true));

        Assert.Equal("ECC.CTR25", ex.Id);
    }

    // ResourceId vazio é rejeitado com ECC.CTR26.
    [Fact]
    public void Constructor_WithEmptyResource_ShouldThrowECC_CTR26()
    {
        var ex = Assert.Throws<DomainException>(
            () => new ContractCharge(CommitmentPurpose.Condominium, ValidAmount(), EconomicResourceId.Empty, RecipientId, collectedByCounterparty: true));

        Assert.Equal("ECC.CTR26", ex.Id);
    }

    // RecipientAgentId vazio é rejeitado com ECC.CTR27.
    [Fact]
    public void Constructor_WithEmptyRecipient_ShouldThrowECC_CTR27()
    {
        var ex = Assert.Throws<DomainException>(
            () => new ContractCharge(CommitmentPurpose.Condominium, ValidAmount(), ResourceId, EconomicAgentId.Empty, collectedByCounterparty: true));

        Assert.Equal("ECC.CTR27", ex.Id);
    }

    // Igualdade por valor: dois charges com os mesmos componentes são iguais.
    [Fact]
    public void Equality_WithSameComponents_ShouldBeEqual()
    {
        var a = new ContractCharge(CommitmentPurpose.Condominium, ValidAmount(), ResourceId, RecipientId, collectedByCounterparty: true);
        var b = new ContractCharge(CommitmentPurpose.Condominium, ValidAmount(), ResourceId, RecipientId, collectedByCounterparty: true);

        Assert.Equal(a, b);
    }

    // Igualdade por valor: charges com flag de cobrança diferente não são iguais.
    [Fact]
    public void Equality_WithDifferentCollectedFlag_ShouldNotBeEqual()
    {
        var passThrough = new ContractCharge(CommitmentPurpose.Condominium, ValidAmount(), ResourceId, RecipientId, collectedByCounterparty: true);
        var direct = new ContractCharge(CommitmentPurpose.Condominium, ValidAmount(), ResourceId, RecipientId, collectedByCounterparty: false);

        Assert.NotEqual(passThrough, direct);
    }
}
