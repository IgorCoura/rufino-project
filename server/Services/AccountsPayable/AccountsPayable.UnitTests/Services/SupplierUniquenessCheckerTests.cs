namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Services;
using AccountsPayable.Domain.Suppliers.ValueObjects;
using AccountsPayable.UnitTests.Services._TestDoubles;
using AccountsPayable.UnitTests.Suppliers.Mothers;

public class SupplierUniquenessCheckerTests
{
    private static readonly TenantId TENANT = SupplierMother.DEFAULT_TENANT;

    // EnsureUniqueAsync passa quando o lookup reporta que o TaxId NÃO está registrado no tenant.
    [Fact]
    public async Task EnsureUniqueAsync_WhenTaxIdNotRegistered_ShouldSucceed()
    {
        var taxId = new TaxId(SupplierMother.VALID_CNPJ);
        var lookup = new FakeSupplierTaxIdLookup();
        var checker = new SupplierUniquenessChecker();

        await checker.EnsureUniqueAsync(TENANT, taxId, lookup);

        Assert.Equal(1, lookup.Calls);
    }

    // EnsureUniqueAsync lança AP.SUC01 quando o lookup reporta que o TaxId já existe no tenant.
    [Fact]
    public async Task EnsureUniqueAsync_WhenTaxIdAlreadyRegistered_ShouldThrowDomainException()
    {
        var taxId = new TaxId(SupplierMother.VALID_CNPJ);
        var lookup = new FakeSupplierTaxIdLookup();
        lookup.Register(TENANT, taxId);
        var checker = new SupplierUniquenessChecker();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => checker.EnsureUniqueAsync(TENANT, taxId, lookup));

        Assert.Equal("AP.SUC01", ex.Id);
    }

    // EnsureUniqueAsync isola por tenant: TaxId registrado em outro tenant não bloqueia o atual.
    [Fact]
    public async Task EnsureUniqueAsync_WhenTaxIdRegisteredInOtherTenant_ShouldSucceed()
    {
        var taxId = new TaxId(SupplierMother.VALID_CNPJ);
        var otherTenant = TenantId.From(new Guid("99999999-9999-9999-9999-999999999999"));
        var lookup = new FakeSupplierTaxIdLookup();
        lookup.Register(otherTenant, taxId);
        var checker = new SupplierUniquenessChecker();

        await checker.EnsureUniqueAsync(TENANT, taxId, lookup);

        Assert.Equal(1, lookup.Calls);
    }

    // EnsureUniqueAsync com TaxId null lança ArgumentNullException (guarda básica do service).
    [Fact]
    public async Task EnsureUniqueAsync_WithNullTaxId_ShouldThrowArgumentNullException()
    {
        var lookup = new FakeSupplierTaxIdLookup();
        var checker = new SupplierUniquenessChecker();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => checker.EnsureUniqueAsync(TENANT, null!, lookup));
    }

    // EnsureUniqueAsync com lookup null lança ArgumentNullException (guarda básica do service).
    [Fact]
    public async Task EnsureUniqueAsync_WithNullLookup_ShouldThrowArgumentNullException()
    {
        var taxId = new TaxId(SupplierMother.VALID_CNPJ);
        var checker = new SupplierUniquenessChecker();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => checker.EnsureUniqueAsync(TENANT, taxId, null!));
    }

    // Mensagem mascara o TaxId — quando lança, o MaskedValue é repassado para o Parameters (não o valor cru).
    [Fact]
    public async Task EnsureUniqueAsync_WhenTaxIdAlreadyRegistered_ShouldUseMaskedTaxIdInParameters()
    {
        var taxId = new TaxId(SupplierMother.VALID_CNPJ);
        var lookup = new FakeSupplierTaxIdLookup();
        lookup.Register(TENANT, taxId);
        var checker = new SupplierUniquenessChecker();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => checker.EnsureUniqueAsync(TENANT, taxId, lookup));

        Assert.Equal("CNPJ", ex.Parameters[0]);
        Assert.Equal(taxId.MaskedValue, ex.Parameters[1]);
        Assert.DoesNotContain(taxId.Value, ex.Message); // valor cru não vaza
    }
}
