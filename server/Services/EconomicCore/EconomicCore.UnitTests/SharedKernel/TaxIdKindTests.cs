namespace EconomicCore.UnitTests.SharedKernel;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class TaxIdKindTests
{
    // GetAll retorna CPF e CNPJ (as duas únicas instâncias).
    [Fact]
    public void GetAll_ShouldReturnCpfAndCnpj()
    {
        var all = Enumeration.GetAll<TaxIdKind>().ToList();

        Assert.Equal(2, all.Count);
        Assert.Contains(TaxIdKind.CPF, all);
        Assert.Contains(TaxIdKind.CNPJ, all);
    }

    // CPF deve ter Id=1, Name="CPF" e ExpectedLength=11 (dígitos do CPF brasileiro).
    [Fact]
    public void Cpf_ShouldHaveExpectedMetadata()
    {
        Assert.Equal(1, TaxIdKind.CPF.Id);
        Assert.Equal("CPF", TaxIdKind.CPF.Name);
        Assert.Equal(11, TaxIdKind.CPF.ExpectedLength);
    }

    // CNPJ deve ter Id=2, Name="CNPJ" e ExpectedLength=14 (dígitos do CNPJ brasileiro).
    [Fact]
    public void Cnpj_ShouldHaveExpectedMetadata()
    {
        Assert.Equal(2, TaxIdKind.CNPJ.Id);
        Assert.Equal("CNPJ", TaxIdKind.CNPJ.Name);
        Assert.Equal(14, TaxIdKind.CNPJ.ExpectedLength);
    }
}
