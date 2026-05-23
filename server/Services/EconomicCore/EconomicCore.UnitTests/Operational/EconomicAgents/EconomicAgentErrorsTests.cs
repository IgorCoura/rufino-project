namespace EconomicCore.UnitTests.Operational.EconomicAgents;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.SeedWork;

public class EconomicAgentErrorsTests
{
    // InvalidName retorna DomainException com Id ECC.AGT01, template e source preenchidos.
    [Fact]
    public void InvalidName_ShouldReturnExceptionWithECC_AGT01()
    {
        var ex = EconomicAgentErrors.InvalidName("", EconomicAgent.NAME_MAX_LENGTH);

        Assert.Equal("ECC.AGT01", ex.Id);
        Assert.False(string.IsNullOrWhiteSpace(ex.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(ex.SourcePath));
        Assert.Equal(2, ex.Parameters.Count);
    }

    // MissingScope retorna DomainException com Id ECC.AGT02 (sem parâmetros).
    [Fact]
    public void MissingScope_ShouldReturnExceptionWithECC_AGT02()
    {
        var ex = EconomicAgentErrors.MissingScope();

        Assert.Equal("ECC.AGT02", ex.Id);
        Assert.False(string.IsNullOrWhiteSpace(ex.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(ex.SourcePath));
        Assert.Empty(ex.Parameters);
    }

    // InvalidTaxId é slot reservado (validação real está no VO TaxId/SHK.TAX*) mas a factory existe e é callable.
    [Fact]
    public void InvalidTaxId_ShouldReturnExceptionWithECC_AGT03()
    {
        var ex = EconomicAgentErrors.InvalidTaxId("invalid-value");

        Assert.Equal("ECC.AGT03", ex.Id);
        Assert.False(string.IsNullOrWhiteSpace(ex.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(ex.SourcePath));
        Assert.Single(ex.Parameters);
    }
}
