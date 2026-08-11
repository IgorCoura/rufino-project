namespace BillPayment.UnitTests.Lookups;

using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;

public class LookupStatusTests
{
    // Só o resultado resolvido carrega retrato; os outros carregam motivo.
    [Fact]
    public void HasSnapshot_ShouldBeTrueOnlyForResolved()
    {
        Assert.True(LookupStatus.Resolved.HasSnapshot);
        Assert.False(LookupStatus.Unresolved.HasSnapshot);
        Assert.False(LookupStatus.Unavailable.HasSnapshot);
    }

    // A distinção que justifica o tipo existir: "não conheço este título" não melhora com
    // retentativa; "não respondi" melhora.
    [Fact]
    public void IsRetryable_ShouldBeTrueOnlyForUnavailable()
    {
        Assert.True(LookupStatus.Unavailable.IsRetryable);
        Assert.False(LookupStatus.Unresolved.IsRetryable);
        Assert.False(LookupStatus.Resolved.IsRetryable);
    }

    // Os três estados estão declarados e são recuperáveis pelo id gravado.
    [Theory]
    [InlineData(1, "Resolved")]
    [InlineData(2, "Unresolved")]
    [InlineData(3, "Unavailable")]
    public void FromValue_ShouldResolveEveryDeclaredStatus(int id, string expectedName)
    {
        Assert.Equal(expectedName, Enumeration.FromValue<LookupStatus>(id).Name);
    }

    // Nenhum estado além dos três — acrescentar um exige revisar quem decide por HasSnapshot.
    [Fact]
    public void GetAll_ShouldDeclareExactlyThreeStatuses()
    {
        Assert.Equal(3, Enumeration.GetAll<LookupStatus>().Count());
    }
}
