namespace BillPayment.UnitTests.TrustedOrigins;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.TrustedOrigins;

public class OriginKindTests
{
    // Só EmailAddress exige '@' no valor cadastrado; os dois tipos de domínio não.
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    public void RequiresAtSign_ShouldBeTrueOnlyForEmailAddress(int id, bool expected)
    {
        var kind = Enumeration.FromValue<OriginKind>(id);

        Assert.Equal(expected, kind.RequiresAtSign);
    }

    // Endereço exato tem precedência sobre domínio na resolução da origem de uma mensagem.
    [Fact]
    public void MatchPrecedence_EmailAddress_ShouldOutrankEmailDomain()
    {
        Assert.True(OriginKind.EmailAddress.MatchPrecedence < OriginKind.EmailDomain.MatchPrecedence);
    }

    // Os três tipos de origem estão registrados e são recuperáveis pelo nome.
    [Theory]
    [InlineData("EmailAddress")]
    [InlineData("EmailDomain")]
    [InlineData("WebDomain")]
    public void FromDisplayName_WithKnownName_ShouldResolveKind(string name)
    {
        var kind = Enumeration.FromDisplayName<OriginKind>(name);

        Assert.Equal(name, kind.Name);
    }

    // O catálogo tem exatamente três tipos — acrescentar um exige revisar a escada de resolução.
    [Fact]
    public void GetAll_ShouldReturnThreeKinds()
    {
        Assert.Equal(3, Enumeration.GetAll<OriginKind>().Count());
    }
}
