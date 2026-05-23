namespace EconomicCore.UnitTests.Operational.EconomicResources;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.SeedWork;

public class EconomicResourceErrorsTests
{
    // InvalidName retorna DomainException com Id ECC.RES01, template e source preenchidos, 2 parâmetros.
    [Fact]
    public void InvalidName_ShouldReturnExceptionWithECC_RES01()
    {
        var ex = EconomicResourceErrors.InvalidName("", EconomicResource.NAME_MAX_LENGTH);

        Assert.Equal("ECC.RES01", ex.Id);
        Assert.False(string.IsNullOrWhiteSpace(ex.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(ex.SourcePath));
        Assert.Equal(2, ex.Parameters.Count);
    }

    // MissingKind retorna DomainException com Id ECC.RES02 (sem parâmetros).
    [Fact]
    public void MissingKind_ShouldReturnExceptionWithECC_RES02()
    {
        var ex = EconomicResourceErrors.MissingKind();

        Assert.Equal("ECC.RES02", ex.Id);
        Assert.False(string.IsNullOrWhiteSpace(ex.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(ex.SourcePath));
        Assert.Empty(ex.Parameters);
    }

    // CustodianMustBeInternal é slot reservado (Domain Service futuro chamará) — a factory existe e é callable.
    [Fact]
    public void CustodianMustBeInternal_ShouldReturnExceptionWithECC_RES03()
    {
        var custodianId = EconomicAgentId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        var ex = EconomicResourceErrors.CustodianMustBeInternal(custodianId);

        Assert.Equal("ECC.RES03", ex.Id);
        Assert.False(string.IsNullOrWhiteSpace(ex.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(ex.SourcePath));
        Assert.Single(ex.Parameters);
    }
}
