namespace TenantManagement.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using TenantManagement.IntegrationTests.Contracts;
using TenantManagement.IntegrationTests.Infrastructure;
using TenantManagement.IntegrationTests.Mothers;

public class ListTenantsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task SeedAsync()
    {
        var client = CreateAdminClient();
        await client.PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(products: ["BillPayment"]), Json);

        await CreateAdminClient().PostAsJsonAsync(
            "api/v1/tenants",
            TenantRequestMother.Individual(),
            Json);

        await CreateAdminClient().PostAsJsonAsync(
            "api/v1/tenants",
            TenantRequestMother.Company(
                legalName: "DESPACON SERVICOS LTDA",
                taxId: TenantRequestMother.OtherCnpj,
                ownerEmail: "contato@despacon.com.br",
                tradeName: "DESPACON"),
            Json);
    }

    // A listagem devolve todos os tenants, com o documento formatado para leitura humana.
    [Fact]
    public async Task GetTenants_ShouldReturnEveryTenant()
    {
        await SeedAsync();

        var response = await CreateAdminClient().GetAsync(new Uri("api/v1/tenants", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadAsync<TenantPageResponse>(response);
        Assert.Equal(3, page.Items.Count);
        Assert.Contains(page.Items, i => i.PrimaryTaxId == "11.222.333/0001-81");
    }

    // O filtro por tipo separa pessoa física de jurídica.
    [Theory]
    [InlineData("Individual", 1)]
    [InlineData("Company", 2)]
    public async Task GetTenants_FilteredByKind_ShouldReturnOnlyThatKind(string kind, int expected)
    {
        await SeedAsync();

        var response = await CreateAdminClient().GetAsync(new Uri($"api/v1/tenants?kind={kind}", UriKind.Relative));

        var page = await ReadAsync<TenantPageResponse>(response);
        Assert.Equal(expected, page.Items.Count);
        Assert.All(page.Items, i => Assert.Equal(kind, i.Kind));
    }

    // O filtro por produto só traz quem tem o produto habilitado.
    [Fact]
    public async Task GetTenants_FilteredByProduct_ShouldReturnOnlySubscribers()
    {
        await SeedAsync();

        var response = await CreateAdminClient().GetAsync(
            new Uri("api/v1/tenants?product=BillPayment", UriKind.Relative));

        var item = Assert.Single((await ReadAsync<TenantPageResponse>(response)).Items);
        Assert.Contains("BillPayment", item.ActiveProducts);
    }

    // A busca casa por nome e por documento — é como um atendente procura pelo telefone.
    // Regressão: buscar por CNPJ derrubava a consulta. O documento é um Value Object convertido
    // para coluna de texto, e navegar até o campo dele (t.PrimaryTaxId.Value) não é traduzível
    // para SQL — a busca por documento nunca achava ninguém e devolvia 400.
    [Theory]
    [InlineData("despacon", 1)]
    [InlineData("11222333", 1)]
    [InlineData("rufino", 1)]
    public async Task GetTenants_WithSearch_ShouldMatchNameOrTaxId(string term, int expected)
    {
        await SeedAsync();

        var response = await CreateAdminClient().GetAsync(new Uri($"api/v1/tenants?search={term}", UriKind.Relative));

        Assert.Equal(expected, (await ReadAsync<TenantPageResponse>(response)).Items.Count);
    }

    // Filtro com valor desconhecido vira 400 com código, em vez de devolver a lista inteira.
    [Fact]
    public async Task GetTenants_WithUnknownStatus_ShouldReturnBadRequestWith_TNM_TNT26()
    {
        await SeedAsync();

        var response = await CreateAdminClient().GetAsync(
            new Uri("api/v1/tenants?status=Cancelado", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TNM.TNT26", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // A paginação por cursor percorre a lista inteira sem repetir nem pular ninguém.
    [Fact]
    public async Task GetTenants_WithCursor_ShouldWalkEveryPageExactlyOnce()
    {
        await SeedAsync();
        var client = CreateAdminClient();
        var seen = new List<Guid>();
        string? cursor = null;

        do
        {
            var url = cursor is null
                ? "api/v1/tenants?limit=1"
                : $"api/v1/tenants?limit=1&cursor={Uri.EscapeDataString(cursor)}";

            var page = await ReadAsync<TenantPageResponse>(await client.GetAsync(new Uri(url, UriKind.Relative)));
            seen.AddRange(page.Items.Select(i => i.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(3, seen.Count);
        Assert.Equal(3, seen.Distinct().Count());
    }
}
