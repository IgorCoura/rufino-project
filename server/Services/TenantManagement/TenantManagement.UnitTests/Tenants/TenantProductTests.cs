namespace TenantManagement.UnitTests.Tenants;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using TenantManagement.UnitTests.Tenants.Mothers;

public class TenantProductTests
{
    // Habilitar um produto registra a data e emite ProductActivated com o nome do produto.
    [Fact]
    public void ActivateProduct_WhenNotEnabled_ShouldEnableAndEmitEvent()
    {
        var tenant = TenantMother.Provisioned();

        tenant.ActivateProduct(ProductCode.BillPayment, TenantMother.DefaultOccurredAt);

        var product = Assert.Single(tenant.Products);
        Assert.Equal(ProductCode.BillPayment, product.Code);
        Assert.True(product.IsActive);
        Assert.Equal(TenantMother.DefaultOccurredAt, product.ActivatedAt);
        Assert.True(tenant.HasActiveProduct(ProductCode.BillPayment));

        var activated = Assert.IsType<ProductActivatedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
        Assert.Equal(nameof(ProductCode.BillPayment), activated.Product);
    }

    // Habilitar duas vezes o mesmo produto não duplica a linha nem emite evento de novo.
    [Fact]
    public void ActivateProduct_WhenAlreadyEnabled_ShouldBeIdempotent()
    {
        var tenant = TenantMother.Provisioned();
        tenant.ActivateProduct(ProductCode.BillPayment, TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();

        tenant.ActivateProduct(ProductCode.BillPayment, TenantMother.DefaultOccurredAt);

        Assert.Single(tenant.Products);
        Assert.Empty(tenant.PullDomainEvents());
    }

    // Desabilitar mantém a linha como histórico, com a data de desligamento.
    [Fact]
    public void DeactivateProduct_WhenEnabled_ShouldKeepHistory()
    {
        var tenant = TenantMother.Provisioned();
        tenant.ActivateProduct(ProductCode.PeopleManagement, TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();
        var later = TenantMother.DefaultOccurredAt.AddDays(5);

        tenant.DeactivateProduct(ProductCode.PeopleManagement, later);

        var product = Assert.Single(tenant.Products);
        Assert.False(product.IsActive);
        Assert.Equal(later, product.DeactivatedAt);
        Assert.False(tenant.HasActiveProduct(ProductCode.PeopleManagement));
        Assert.IsType<ProductDeactivatedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
    }

    // Reabilitar um produto desligado reaproveita a linha e limpa a data de desligamento.
    [Fact]
    public void ActivateProduct_AfterDeactivation_ShouldReuseTheSameRow()
    {
        var tenant = TenantMother.Provisioned();
        tenant.ActivateProduct(ProductCode.BillPayment, TenantMother.DefaultOccurredAt);
        tenant.DeactivateProduct(ProductCode.BillPayment, TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();
        var later = TenantMother.DefaultOccurredAt.AddDays(10);

        tenant.ActivateProduct(ProductCode.BillPayment, later);

        var product = Assert.Single(tenant.Products);
        Assert.True(product.IsActive);
        Assert.Null(product.DeactivatedAt);
        Assert.Equal(later, product.ActivatedAt);
        Assert.IsType<ProductActivatedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
    }

    // Desabilitar produto que não está habilitado é reprovado em TNM.TNT17.
    [Fact]
    public void DeactivateProduct_WhenNotEnabled_ShouldThrow_TNM_TNT17()
    {
        var tenant = TenantMother.Provisioned();

        var error = Assert.Throws<DomainException>(() =>
            tenant.DeactivateProduct(ProductCode.BillPayment, TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT17", error.Id);
    }

    // Habilitar produto sem informar qual é reprovado em TNM.TNT16.
    [Fact]
    public void ActivateProduct_WithoutProduct_ShouldThrow_TNM_TNT16()
    {
        var tenant = TenantMother.Provisioned();

        var error = Assert.Throws<DomainException>(() =>
            tenant.ActivateProduct(null!, TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT16", error.Id);
    }

    // Tenant suspenso não habilita produto novo (TNM.TNT12).
    [Fact]
    public void ActivateProduct_WhenSuspended_ShouldThrow_TNM_TNT12()
    {
        var tenant = TenantMother.Provisioned();
        tenant.Suspend("Inadimplência", TenantMother.DefaultOccurredAt);

        var error = Assert.Throws<DomainException>(() =>
            tenant.ActivateProduct(ProductCode.BillPayment, TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT12", error.Id);
    }

    // Os dois produtos convivem no mesmo tenant — é o caso do cliente que usa RH e contas a pagar.
    [Fact]
    public void ActivateProduct_ForBothProducts_ShouldKeepBothEnabled()
    {
        var tenant = TenantMother.Provisioned();

        tenant.ActivateProduct(ProductCode.PeopleManagement, TenantMother.DefaultOccurredAt);
        tenant.ActivateProduct(ProductCode.BillPayment, TenantMother.DefaultOccurredAt);

        Assert.Equal(2, tenant.Products.Count);
        Assert.True(tenant.HasActiveProduct(ProductCode.PeopleManagement));
        Assert.True(tenant.HasActiveProduct(ProductCode.BillPayment));
    }
}
