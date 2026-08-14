namespace TenantManagement.UnitTests.Tenants;

using System.Text.RegularExpressions;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;

/// <summary>
/// O Id do erro é contrato com quem consome a API e com o log. Estes testes existem para
/// que renumerar ou renomear um erro seja uma decisão, não um efeito colateral.
/// </summary>
public class TenantErrorsTests
{
    // Todo erro do Aggregate carrega Id no padrão TNM.TNT##, mensagem e origem no código.
    [Theory]
    [MemberData(nameof(AllErrors))]
    public void EveryError_ShouldCarryIdMessageAndSource(string expectedId, DomainException error)
    {
        Assert.Equal(expectedId, error.Id);
        Assert.Matches(new Regex(@"^TNM\.TNT\d+$"), error.Id);
        Assert.False(string.IsNullOrWhiteSpace(error.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(error.SourcePath));
    }

    // Conflito e ausência têm categoria própria — é o que o filtro da API traduz em 409 e 404.
    [Fact]
    public void Errors_ShouldCarryTheRightCategory()
    {
        Assert.Equal(DomainErrorCategory.Conflict, TenantErrors.TaxIdAlreadyRegistered("11222333000181").Category);
        Assert.Equal(DomainErrorCategory.Conflict, TenantErrors.LastOwnerCannotBeRevoked().Category);
        Assert.Equal(DomainErrorCategory.NotFound, TenantErrors.NotFound(Guid.Empty).Category);
        Assert.Equal(DomainErrorCategory.NotFound, TenantErrors.MembershipNotFound("a@b.com").Category);
        Assert.Equal(DomainErrorCategory.Validation, TenantErrors.LegalNameRequired().Category);
    }

    // Os parâmetros entram na mensagem final, e não ficam só guardados.
    [Fact]
    public void Error_WithParameters_ShouldInterpolateThem()
    {
        var error = TenantErrors.PrimaryTaxIdKindMismatch("Company", "CNPJ", "CPF");

        Assert.Contains("Company", error.Message, StringComparison.Ordinal);
        Assert.Contains("CNPJ", error.Message, StringComparison.Ordinal);
        Assert.Contains("CPF", error.Message, StringComparison.Ordinal);
    }

    public static TheoryData<string, DomainException> AllErrors() => new()
    {
        { "TNM.TNT01", TenantErrors.KindRequired() },
        { "TNM.TNT02", TenantErrors.LegalNameRequired() },
        { "TNM.TNT03", TenantErrors.LegalNameTooLong(200) },
        { "TNM.TNT04", TenantErrors.PrimaryTaxIdRequired() },
        { "TNM.TNT05", TenantErrors.PrimaryTaxIdKindMismatch("Company", "CNPJ", "CPF") },
        { "TNM.TNT06", TenantErrors.TradeNameRequiresCompany() },
        { "TNM.TNT07", TenantErrors.TradeNameTooLong(200) },
        { "TNM.TNT08", TenantErrors.ContactRequired() },
        { "TNM.TNT09", TenantErrors.AddressRequired() },
        { "TNM.TNT10", TenantErrors.TaxIdAlreadyRegistered("11222333000181") },
        { "TNM.TNT11", TenantErrors.NotFound(Guid.Empty) },
        { "TNM.TNT12", TenantErrors.SuspendedTenantIsReadOnly() },
        { "TNM.TNT13", TenantErrors.AlreadySuspended() },
        { "TNM.TNT14", TenantErrors.NotSuspended() },
        { "TNM.TNT15", TenantErrors.SuspensionReasonRequired() },
        { "TNM.TNT16", TenantErrors.ProductRequired() },
        { "TNM.TNT17", TenantErrors.ProductNotActive("BillPayment") },
        { "TNM.TNT18", TenantErrors.MembershipEmailRequired() },
        { "TNM.TNT19", TenantErrors.InvalidMembershipEmail("nao-e-email") },
        { "TNM.TNT20", TenantErrors.LastOwnerCannotBeRevoked() },
        { "TNM.TNT21", TenantErrors.MembershipNotFound("a@b.com") },
        { "TNM.TNT22", TenantErrors.MembershipRoleRequired() },
        { "TNM.TNT23", TenantErrors.UnknownKind("PessoaFisica") },
        { "TNM.TNT24", TenantErrors.UnknownProduct("Folha") },
        { "TNM.TNT25", TenantErrors.UnknownMembershipRole("Admin") },
        { "TNM.TNT26", TenantErrors.UnknownStatus("Cancelado") },
    };
}
