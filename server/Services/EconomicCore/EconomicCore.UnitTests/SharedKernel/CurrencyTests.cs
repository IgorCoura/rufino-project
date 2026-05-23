namespace EconomicCore.UnitTests.SharedKernel;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class CurrencyTests
{
    // BRL é a única Currency cadastrada — GetAll deve retornar exatamente 1 item.
    [Fact]
    public void GetAll_ShouldReturnSingleBrlInstance()
    {
        var all = Enumeration.GetAll<Currency>().ToList();

        Assert.Single(all);
        Assert.Same(Currency.BRL, all[0]);
    }

    // BRL deve estar registrada com Id=1 e Name="BRL".
    [Fact]
    public void Brl_ShouldHaveIdOneAndNameBrl()
    {
        Assert.Equal(1, Currency.BRL.Id);
        Assert.Equal("BRL", Currency.BRL.Name);
    }

    // FromValue(1) deve resolver para a instância BRL.
    [Fact]
    public void FromValue_WithOne_ShouldReturnBrl()
    {
        var result = Enumeration.FromValue<Currency>(1);

        Assert.Same(Currency.BRL, result);
    }

    // FromDisplayName("BRL") deve resolver para a instância BRL (case-insensitive).
    [Fact]
    public void FromDisplayName_WithBrl_ShouldReturnBrl()
    {
        var result = Enumeration.FromDisplayName<Currency>("BRL");

        Assert.Same(Currency.BRL, result);
    }

    // TryFromValue com Id inexistente retorna null sem lançar.
    [Fact]
    public void TryFromValue_WithUnknownId_ShouldReturnNull()
    {
        var result = Enumeration.TryFromValue<Currency>(99);

        Assert.Null(result);
    }
}
