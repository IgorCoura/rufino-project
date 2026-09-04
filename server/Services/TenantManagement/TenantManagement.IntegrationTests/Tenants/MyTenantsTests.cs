namespace TenantManagement.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using TenantManagement.IntegrationTests.Contracts;
using TenantManagement.IntegrationTests.Infrastructure;
using TenantManagement.IntegrationTests.Mothers;

public class MyTenantsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    // A consulta parte do e-mail do próprio token e devolve os tenants da pessoa, com o papel dela.
    [Fact]
    public async Task GetMyTenants_ShouldReturnOnlyTheTenantsOfTheAuthenticatedPerson()
    {
        var admin = CreateAdminClient();
        var mine = await ReadAsync<IdResponse>(await admin.PostAsJsonAsync(
            "api/v1/tenants",
            TenantRequestMother.Company(products: ["BillPayment"]),
            Json));

        await CreateAdminClient().PostAsJsonAsync(
            "api/v1/tenants",
            TenantRequestMother.Company(
                legalName: "DESPACON SERVICOS LTDA",
                taxId: TenantRequestMother.OtherCnpj,
                ownerEmail: "contato@despacon.com.br"),
            Json);

        var client = CreateMemberClient(TenantRequestMother.OwnerEmail);
        var response = await client.GetAsync(new Uri("api/v1/me/tenants", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenants = await ReadAsync<IReadOnlyList<MyTenantResponse>>(response);
        var tenant = Assert.Single(tenants);
        Assert.Equal(mine.Id, tenant.Id);
        Assert.Equal("Owner", tenant.Role);
        Assert.Contains("BillPayment", tenant.ActiveProducts);
    }

    // Quem perdeu o acesso some da lista — a consulta considera só vínculo ativo.
    [Fact]
    public async Task GetMyTenants_AfterRevocation_ShouldNotListTheTenantAnymore()
    {
        var admin = CreateAdminClient();
        var created = await ReadAsync<IdResponse>(
            await admin.PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(), Json));

        const string second = "socio@rufino.com.br";
        await CreateAdminClient().PostAsJsonAsync(
            $"api/v1/tenants/{created.Id}/members", new GrantMembershipRequest(second, "Member"), Json);
        await CreateAdminClient().DeleteAsync(
            new Uri($"api/v1/tenants/{created.Id}/members?email={second}", UriKind.Relative));

        var response = await CreateMemberClient(second).GetAsync(new Uri("api/v1/me/tenants", UriKind.Relative));

        Assert.Empty(await ReadAsync<IReadOnlyList<MyTenantResponse>>(response));
    }

    // Sem token não há consulta: o endpoint exige autenticação.
    [Fact]
    public async Task GetMyTenants_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await Factory.CreateClient().GetAsync(new Uri("api/v1/me/tenants", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
