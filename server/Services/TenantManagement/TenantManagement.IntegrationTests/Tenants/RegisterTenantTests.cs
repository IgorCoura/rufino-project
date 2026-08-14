namespace TenantManagement.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using TenantManagement.Domain.Tenants;
using TenantManagement.IntegrationTests.Contracts;
using TenantManagement.IntegrationTests.Infrastructure;
using TenantManagement.IntegrationTests.Mothers;

public class RegisterTenantTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    // Cadastrar pessoa jurídica grava identidade, endereço e o titular como responsável.
    [Fact]
    public async Task PostTenants_WithCompanyPayload_ShouldPersistTheWholeRegistration()
    {
        var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(), Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await ReadAsync<IdResponse>(response);

        var tenant = await GetTenantAsync(created.Id);
        Assert.NotNull(tenant);
        Assert.Equal(TenantKind.Company, tenant.Kind);
        Assert.Equal(TenantRequestMother.Cnpj, tenant.PrimaryTaxId.Value);
        Assert.Equal("RUFINO", tenant.TradeName);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Equal("AVENIDA PAULISTA", tenant.Address.Street);
        Assert.Equal(TenantRequestMother.OwnerEmail, Assert.Single(tenant.Memberships).Email);
    }

    // Pessoa física entra pelo mesmo endpoint, com CPF — é a razão de este BC existir.
    [Fact]
    public async Task PostTenants_WithIndividualPayload_ShouldAcceptCpf()
    {
        var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Individual(), Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await GetTenantAsync((await ReadAsync<IdResponse>(response)).Id);

        Assert.NotNull(tenant);
        Assert.Equal(TenantKind.Individual, tenant.Kind);
        Assert.Equal(TenantRequestMother.Cpf, tenant.PrimaryTaxId.Value);
        Assert.Equal(string.Empty, tenant.TradeName);
    }

    // O CEP é gravado só com dígitos e o endereço em caixa alta, venha como vier.
    [Fact]
    public async Task PostTenants_WithMaskedZipCode_ShouldNormalizeIt()
    {
        var client = CreateAdminClient();
        var request = TenantRequestMother.Company(address: TenantRequestMother.Address("30130-010", "Belo Horizonte", "mg"));

        var response = await client.PostAsJsonAsync("api/v1/tenants", request, Json);

        var tenant = await GetTenantAsync((await ReadAsync<IdResponse>(response)).Id);
        Assert.NotNull(tenant);
        Assert.Equal("30130010", tenant.Address.ZipCode);
        Assert.Equal("BELO HORIZONTE", tenant.Address.City);
        Assert.Equal("MG", tenant.Address.State);
    }

    // Pessoa jurídica com CPF é recusada com 400 e o código TNM.TNT05.
    [Fact]
    public async Task PostTenants_CompanyWithCpf_ShouldReturnBadRequestWith_TNM_TNT05()
    {
        var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            "api/v1/tenants",
            TenantRequestMother.Company(taxId: TenantRequestMother.Cpf),
            Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TNM.TNT05", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // Documento com dígito verificador errado não entra no cadastro (TNM.TAX03).
    [Fact]
    public async Task PostTenants_WithInvalidCheckDigit_ShouldReturnBadRequest()
    {
        var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            "api/v1/tenants",
            TenantRequestMother.Company(taxId: "11222333000182"),
            Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TNM.TAX03", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // Endereço sem cidade é recusado: o cadastro exige endereço completo desde o primeiro dia.
    [Fact]
    public async Task PostTenants_WithIncompleteAddress_ShouldReturnBadRequestWith_TNM_ADR02()
    {
        var client = CreateAdminClient();
        var request = TenantRequestMother.Company(address: TenantRequestMother.Address(city: ""));

        var response = await client.PostAsJsonAsync("api/v1/tenants", request, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TNM.ADR02", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // O mesmo documento em dois tenants é conflito (409, TNM.TNT10) — um documento, um tenant.
    [Fact]
    public async Task PostTenants_WithDuplicateTaxId_ShouldReturnConflictWith_TNM_TNT10()
    {
        var client = CreateAdminClient();
        await client.PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(), Json);

        var second = CreateAdminClient();
        var response = await second.PostAsJsonAsync(
            "api/v1/tenants",
            TenantRequestMother.Company(legalName: "OUTRA EMPRESA LTDA", ownerEmail: "outro@empresa.com.br"),
            Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("TNM.TNT10", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // Tipo de tenant desconhecido vira 400 com código, não 500.
    [Fact]
    public async Task PostTenants_WithUnknownKind_ShouldReturnBadRequestWith_TNM_TNT23()
    {
        var client = CreateAdminClient();
        var request = TenantRequestMother.Company() with { Kind = "PessoaFisica" };

        var response = await client.PostAsJsonAsync("api/v1/tenants", request, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TNM.TNT23", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // O Id informado é preservado — é o que permite migrar um cadastro sem reemitir acesso.
    [Fact]
    public async Task PostTenants_WithInformedId_ShouldPreserveIt()
    {
        var id = new Guid("0195a1f0-0000-7000-8000-00000000beef");
        var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(id: id), Json);

        Assert.Equal(id, (await ReadAsync<IdResponse>(response)).Id);
        Assert.NotNull(await GetTenantAsync(id));
    }

    // Produtos informados no cadastro já nascem habilitados.
    [Fact]
    public async Task PostTenants_WithProducts_ShouldEnableThem()
    {
        var client = CreateAdminClient();
        var request = TenantRequestMother.Company(products: ["BillPayment", "PeopleManagement"]);

        var response = await client.PostAsJsonAsync("api/v1/tenants", request, Json);

        var tenant = await GetTenantAsync((await ReadAsync<IdResponse>(response)).Id);
        Assert.NotNull(tenant);
        Assert.Equal(2, tenant.Products.Count);
        Assert.True(tenant.HasActiveProduct(ProductCode.BillPayment));
    }

    // O mesmo x-requestid duas vezes cadastra uma vez só — idempotência do header.
    [Fact]
    public async Task PostTenants_WithSameRequestId_ShouldRegisterOnlyOnce()
    {
        var requestId = new Guid("0195a1f0-0000-7000-8000-000000000f01");
        var client = CreateAdminClient(requestId);
        var request = TenantRequestMother.Company();

        var first = await client.PostAsJsonAsync("api/v1/tenants", request, Json);
        var second = await client.PostAsJsonAsync("api/v1/tenants", request, Json);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.NotEqual(Guid.Empty, (await ReadAsync<IdResponse>(first)).Id);
        Assert.Equal(Guid.Empty, (await ReadAsync<IdResponse>(second)).Id);

        var count = await Factory.ExecuteDbContextAsync(db => Task.FromResult(db.Tenants.Count()));
        Assert.Equal(1, count);
    }

    // Consultar um tenant que não existe devolve 404, não 500 nem corpo vazio com 200.
    [Fact]
    public async Task GetTenant_WhenNotFound_ShouldReturnNotFound()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync(new Uri($"api/v1/tenants/{Guid.NewGuid()}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // A leitura devolve o cadastro completo, com endereço, contato e vínculos.
    [Fact]
    public async Task GetTenant_AfterRegistration_ShouldReturnTheWholeRegistration()
    {
        var client = CreateAdminClient();
        var created = await ReadAsync<IdResponse>(
            await client.PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(), Json));

        var response = await client.GetAsync(new Uri($"api/v1/tenants/{created.Id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await ReadAsync<TenantResponse>(response);
        Assert.Equal("Company", tenant.Kind);
        Assert.Equal("CNPJ", tenant.PrimaryTaxIdKind);
        Assert.Equal("01310100", tenant.Address.ZipCode);
        Assert.Equal("contato@rufino.com.br", tenant.Contact.Email);
        Assert.Equal(TenantRequestMother.OwnerEmail, Assert.Single(tenant.Memberships).Email);
    }
}
