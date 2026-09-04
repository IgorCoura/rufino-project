namespace TenantManagement.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using TenantManagement.Domain.Tenants;
using TenantManagement.IntegrationTests.Contracts;
using TenantManagement.IntegrationTests.Infrastructure;
using TenantManagement.IntegrationTests.Mothers;

public class TenantLifecycleTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<Guid> RegisterAsync()
        => (await ReadAsync<IdResponse>(
            await CreateAdminClient().PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(), Json))).Id;

    // Editar razão social e nome fantasia grava os dois.
    [Fact]
    public async Task PutTenant_WithNewNames_ShouldPersistThem()
    {
        var id = await RegisterAsync();

        var response = await CreateAdminClient().PutAsJsonAsync(
            $"api/v1/tenants/{id}",
            new EditTenantDetailsRequest("RUFINO ENGENHARIA LTDA", "RUFINO ENG"),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await GetTenantAsync(id);
        Assert.NotNull(tenant);
        Assert.Equal("RUFINO ENGENHARIA LTDA", tenant.LegalName);
        Assert.Equal("RUFINO ENG", tenant.TradeName);
    }

    // Trocar o endereço substitui o VO inteiro, com a mesma normalização do cadastro.
    [Fact]
    public async Task PutAddress_WithNewAddress_ShouldReplaceIt()
    {
        var id = await RegisterAsync();

        var response = await CreateAdminClient().PutAsJsonAsync(
            $"api/v1/tenants/{id}/address",
            new ChangeAddressRequest(TenantRequestMother.Address("30130-010", "Belo Horizonte", "mg")),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await GetTenantAsync(id);
        Assert.NotNull(tenant);
        Assert.Equal("30130010", tenant.Address.ZipCode);
        Assert.Equal("MG", tenant.Address.State);
    }

    // Trocar o contato grava e-mail e telefone normalizados.
    [Fact]
    public async Task PutContact_WithNewContact_ShouldReplaceIt()
    {
        var id = await RegisterAsync();

        var response = await CreateAdminClient().PutAsJsonAsync(
            $"api/v1/tenants/{id}/contact",
            new ChangeContactRequest("Financeiro@Rufino.com.BR", "(11) 3322-4455"),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await GetTenantAsync(id);
        Assert.NotNull(tenant);
        Assert.Equal("financeiro@rufino.com.br", tenant.Contact.Email);
        Assert.Equal("1133224455", tenant.Contact.Phone);
    }

    // Suspender grava o motivo e passa a recusar alteração de cadastro com 409.
    [Fact]
    public async Task PostSuspend_ShouldFreezeTheRegistration()
    {
        var id = await RegisterAsync();

        var suspend = await CreateAdminClient().PostAsJsonAsync(
            $"api/v1/tenants/{id}/suspend", new SuspendTenantRequest("Inadimplência"), Json);

        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal(nameof(TenantStatus.Suspended), (await ReadAsync<StatusResponse>(suspend)).Status);

        var edit = await CreateAdminClient().PutAsJsonAsync(
            $"api/v1/tenants/{id}", new EditTenantDetailsRequest("OUTRO NOME", null), Json);

        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        Assert.Equal("TNM.TNT12", (await ReadAsync<ErrorResponse>(edit)).Id);
    }

    // Suspender sem motivo é recusado — suspensão sem registro é suspensão sem responsável.
    [Fact]
    public async Task PostSuspend_WithoutReason_ShouldReturnBadRequestWith_TNM_TNT15()
    {
        var id = await RegisterAsync();

        var response = await CreateAdminClient().PostAsJsonAsync(
            $"api/v1/tenants/{id}/suspend", new SuspendTenantRequest("  "), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TNM.TNT15", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // Reativar devolve o tenant ao ar e limpa o motivo da suspensão.
    [Fact]
    public async Task PostReactivate_AfterSuspension_ShouldReturnToActive()
    {
        var id = await RegisterAsync();
        await CreateAdminClient().PostAsJsonAsync(
            $"api/v1/tenants/{id}/suspend", new SuspendTenantRequest("Inadimplência"), Json);

        var response = await CreateAdminClient().PostAsync(
            new Uri($"api/v1/tenants/{id}/reactivate", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await GetTenantAsync(id);
        Assert.NotNull(tenant);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Equal(string.Empty, tenant.SuspensionReason);
    }

    // Habilitar e desabilitar produto grava o histórico e o estado corrente.
    [Fact]
    public async Task ProductEndpoints_ShouldEnableAndDisableKeepingHistory()
    {
        var id = await RegisterAsync();

        var activate = await CreateAdminClient().PostAsync(
            new Uri($"api/v1/tenants/{id}/products/BillPayment", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);
        Assert.True((await ReadAsync<ProductResponse>(activate)).IsActive);

        var deactivate = await CreateAdminClient().DeleteAsync(
            new Uri($"api/v1/tenants/{id}/products/BillPayment", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var tenant = await GetTenantAsync(id);
        Assert.NotNull(tenant);
        var product = Assert.Single(tenant.Products);
        Assert.False(product.IsActive);
        Assert.NotNull(product.DeactivatedAt);
    }

    // Desabilitar produto que não está habilitado devolve 409 com TNM.TNT17.
    [Fact]
    public async Task DeleteProduct_WhenNotEnabled_ShouldReturnConflictWith_TNM_TNT17()
    {
        var id = await RegisterAsync();

        var response = await CreateAdminClient().DeleteAsync(
            new Uri($"api/v1/tenants/{id}/products/BillPayment", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("TNM.TNT17", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // Produto desconhecido vira 400 com código em vez de 500.
    [Fact]
    public async Task PostProduct_WithUnknownProduct_ShouldReturnBadRequestWith_TNM_TNT24()
    {
        var id = await RegisterAsync();

        var response = await CreateAdminClient().PostAsync(
            new Uri($"api/v1/tenants/{id}/products/Folha", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TNM.TNT24", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // Operar sobre tenant inexistente devolve 404 com TNM.TNT11.
    [Fact]
    public async Task PutTenant_WhenTenantDoesNotExist_ShouldReturnNotFoundWith_TNM_TNT11()
    {
        var response = await CreateAdminClient().PutAsJsonAsync(
            $"api/v1/tenants/{Guid.NewGuid()}",
            new EditTenantDetailsRequest("QUALQUER NOME", null),
            Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("TNM.TNT11", (await ReadAsync<ErrorResponse>(response)).Id);
    }
}
