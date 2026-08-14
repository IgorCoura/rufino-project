namespace TenantManagement.UnitTests.Tenants;

using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;

public class TenantKindTests
{
    // Pessoa física se cadastra com CPF e não tem nome fantasia; jurídica é o oposto.
    [Fact]
    public void Individual_ShouldExpectCpfAndRejectTradeName()
    {
        Assert.Equal(TaxIdKind.CPF, TenantKind.Individual.ExpectedPrimaryTaxIdKind);
        Assert.False(TenantKind.Individual.AllowsTradeName);
    }

    // Pessoa jurídica se cadastra com CNPJ e pode ter nome fantasia.
    [Fact]
    public void Company_ShouldExpectCnpjAndAllowTradeName()
    {
        Assert.Equal(TaxIdKind.CNPJ, TenantKind.Company.ExpectedPrimaryTaxIdKind);
        Assert.True(TenantKind.Company.AllowsTradeName);
    }

    // Os dois tipos são os únicos que existem — o BC atende PF e PJ no mesmo modelo.
    [Fact]
    public void GetAll_ShouldReturnBothKinds()
    {
        var kinds = TenantKind.GetAll<TenantKind>().ToList();

        Assert.Equal(2, kinds.Count);
        Assert.Contains(TenantKind.Individual, kinds);
        Assert.Contains(TenantKind.Company, kinds);
    }
}
