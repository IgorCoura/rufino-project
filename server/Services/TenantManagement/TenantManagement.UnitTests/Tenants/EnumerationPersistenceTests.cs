namespace TenantManagement.UnitTests.Tenants;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;

/// <summary>
/// Os Smart Enums são persistidos pelo <c>Id</c>. Renumerar um valor não quebra compilação
/// nenhuma — ele reescreve em silêncio o significado de tudo que já está no banco. Estes
/// testes congelam os números.
/// </summary>
public class EnumerationPersistenceTests
{
    // Os Ids gravados de TenantKind não mudam: 1 é pessoa física, 2 é jurídica.
    [Theory]
    [InlineData(1, nameof(TenantKind.Individual))]
    [InlineData(2, nameof(TenantKind.Company))]
    public void TenantKind_ShouldKeepPersistedIds(int id, string expectedName)
    {
        Assert.Equal(expectedName, Enumeration.FromValue<TenantKind>(id).Name);
    }

    // Os Ids gravados de TenantStatus não mudam.
    [Theory]
    [InlineData(1, nameof(TenantStatus.Active))]
    [InlineData(2, nameof(TenantStatus.Suspended))]
    public void TenantStatus_ShouldKeepPersistedIds(int id, string expectedName)
    {
        Assert.Equal(expectedName, Enumeration.FromValue<TenantStatus>(id).Name);
    }

    // Os Ids gravados de MembershipRole não mudam: trocá-los promoveria ou rebaixaria gente calada.
    [Theory]
    [InlineData(1, nameof(MembershipRole.Owner))]
    [InlineData(2, nameof(MembershipRole.Member))]
    public void MembershipRole_ShouldKeepPersistedIds(int id, string expectedName)
    {
        Assert.Equal(expectedName, Enumeration.FromValue<MembershipRole>(id).Name);
    }

    // Os Ids gravados de ProvisioningStatus não mudam.
    [Theory]
    [InlineData(1, nameof(ProvisioningStatus.Pending))]
    [InlineData(2, nameof(ProvisioningStatus.Done))]
    [InlineData(3, nameof(ProvisioningStatus.Failed))]
    public void ProvisioningStatus_ShouldKeepPersistedIds(int id, string expectedName)
    {
        Assert.Equal(expectedName, Enumeration.FromValue<ProvisioningStatus>(id).Name);
    }

    // Os Ids gravados de ProductCode não mudam: trocá-los habilitaria o produto errado.
    [Theory]
    [InlineData(1, nameof(ProductCode.PeopleManagement))]
    [InlineData(2, nameof(ProductCode.BillPayment))]
    public void ProductCode_ShouldKeepPersistedIds(int id, string expectedName)
    {
        Assert.Equal(expectedName, Enumeration.FromValue<ProductCode>(id).Name);
    }

    // Os Ids gravados de TaxIdKind não mudam: 1 é CPF, 2 é CNPJ.
    [Theory]
    [InlineData(1, "CPF")]
    [InlineData(2, "CNPJ")]
    public void TaxIdKind_ShouldKeepPersistedIds(int id, string expectedName)
    {
        Assert.Equal(expectedName, Enumeration.FromValue<TaxIdKind>(id).Name);
    }

    // Buscar por um Id que não existe é erro de programação, não estado de domínio.
    [Fact]
    public void FromValue_WithUnknownId_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => Enumeration.FromValue<TenantStatus>(99));
    }
}
