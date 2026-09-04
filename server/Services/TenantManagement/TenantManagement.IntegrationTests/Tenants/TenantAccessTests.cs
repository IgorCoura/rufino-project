namespace TenantManagement.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using TenantManagement.Domain.Tenants;
using TenantManagement.IntegrationTests.Contracts;
using TenantManagement.IntegrationTests.Infrastructure;
using TenantManagement.IntegrationTests.Mothers;

public class TenantAccessTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string SecondEmail = "socio@rufino.com.br";

    private async Task<Guid> RegisterAsync(HttpClient client)
        => (await ReadAsync<IdResponse>(
            await client.PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(), Json))).Id;

    // Cadastrar concede o acesso do titular e o provedor de identidade confirma: vínculo Done.
    [Fact]
    public async Task PostTenants_WhenProvisionerSucceeds_ShouldMarkMembershipAsDone()
    {
        var client = CreateAdminClient();

        var id = await RegisterAsync(client);

        var tenant = await GetTenantAsync(id);
        Assert.NotNull(tenant);
        var owner = Assert.Single(tenant.Memberships);
        Assert.Equal(ProvisioningStatus.Done, owner.Provisioning);
        Assert.Equal(RecordingTenantAccessProvisioner.UserIdFor(TenantRequestMother.OwnerEmail), owner.IdentityUserId);
        Assert.Contains(Provisioner.Granted, g => g.Email == TenantRequestMother.OwnerEmail);
    }

    // Provedor fora do ar não derruba o cadastro: o tenant existe e o vínculo fica marcado como falho.
    [Fact]
    public async Task PostTenants_WhenProvisionerFails_ShouldStillRegisterAndMarkFailed()
    {
        Provisioner.FailGrants = true;
        var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(), Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await GetTenantAsync((await ReadAsync<IdResponse>(response)).Id);
        Assert.NotNull(tenant);
        Assert.Equal(ProvisioningStatus.Failed, Assert.Single(tenant.Memberships).Provisioning);
        Assert.Equal(ProvisioningStatus.Failed, tenant.AccessProvisioning);
    }

    // Reprovisionar depois que o provedor volta conserta o vínculo falho — é o botão de conserto.
    [Fact]
    public async Task PostReprovision_AfterProvisionerRecovers_ShouldFixTheFailedMembership()
    {
        Provisioner.FailGrants = true;
        var client = CreateAdminClient();
        var id = await RegisterAsync(client);

        Provisioner.FailGrants = false;
        var response = await CreateAdminClient().PostAsync(
            new Uri($"api/v1/tenants/{id}/access/reprovision", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadAsync<ReprovisionResponse>(response);
        Assert.Equal(1, result.RequeuedMemberships);

        var tenant = await GetTenantAsync(id);
        Assert.NotNull(tenant);
        Assert.Equal(ProvisioningStatus.Done, Assert.Single(tenant.Memberships).Provisioning);
    }

    // Reprovisionar quando está tudo certo não faz nada e continua respondendo 200.
    [Fact]
    public async Task PostReprovision_WhenNothingPending_ShouldReportZeroRequeued()
    {
        var client = CreateAdminClient();
        var id = await RegisterAsync(client);

        var response = await CreateAdminClient().PostAsync(
            new Uri($"api/v1/tenants/{id}/access/reprovision", UriKind.Relative), content: null);

        Assert.Equal(0, (await ReadAsync<ReprovisionResponse>(response)).RequeuedMemberships);
    }

    // Conceder acesso a outra pessoa cria o vínculo e o provedor recebe a concessão.
    [Fact]
    public async Task PostMembers_WithNewEmail_ShouldGrantAndProvision()
    {
        var client = CreateAdminClient();
        var id = await RegisterAsync(client);

        var response = await CreateAdminClient().PostAsJsonAsync(
            $"api/v1/tenants/{id}/members",
            new GrantMembershipRequest(SecondEmail, "Member"),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await GetTenantAsync(id);
        Assert.NotNull(tenant);
        Assert.Equal(2, tenant.Memberships.Count);
        Assert.Contains(Provisioner.Granted, g => g.Email == SecondEmail);
    }

    // Revogar corta o acesso e o provedor recebe a revogação.
    [Fact]
    public async Task DeleteMembers_WhenAnotherOwnerExists_ShouldRevokeAndPropagate()
    {
        var client = CreateAdminClient();
        var id = await RegisterAsync(client);
        await CreateAdminClient().PostAsJsonAsync(
            $"api/v1/tenants/{id}/members", new GrantMembershipRequest(SecondEmail, "Owner"), Json);

        var response = await CreateAdminClient().DeleteAsync(
            new Uri($"api/v1/tenants/{id}/members?email={SecondEmail}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await GetTenantAsync(id);
        Assert.NotNull(tenant);
        Assert.False(Assert.Single(tenant.Memberships, m => m.Email == SecondEmail).IsActive);
        Assert.Contains(Provisioner.Revoked, r => r.Email == SecondEmail);
    }

    // Revogar o último responsável é recusado com 409 e TNM.TNT20.
    [Fact]
    public async Task DeleteMembers_WhenLastOwner_ShouldReturnConflictWith_TNM_TNT20()
    {
        var client = CreateAdminClient();
        var id = await RegisterAsync(client);

        var response = await CreateAdminClient().DeleteAsync(
            new Uri($"api/v1/tenants/{id}/members?email={TenantRequestMother.OwnerEmail}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("TNM.TNT20", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // Revogar acesso de quem não tem devolve 404 com TNM.TNT21.
    [Fact]
    public async Task DeleteMembers_ForUnknownEmail_ShouldReturnNotFoundWith_TNM_TNT21()
    {
        var client = CreateAdminClient();
        var id = await RegisterAsync(client);

        var response = await CreateAdminClient().DeleteAsync(
            new Uri($"api/v1/tenants/{id}/members?email=ninguem@rufino.com.br", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("TNM.TNT21", (await ReadAsync<ErrorResponse>(response)).Id);
    }

    // Papel desconhecido vira 400 com código em vez de 500.
    [Fact]
    public async Task PostMembers_WithUnknownRole_ShouldReturnBadRequestWith_TNM_TNT25()
    {
        var client = CreateAdminClient();
        var id = await RegisterAsync(client);

        var response = await CreateAdminClient().PostAsJsonAsync(
            $"api/v1/tenants/{id}/members",
            new GrantMembershipRequest(SecondEmail, "Administrador"),
            Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TNM.TNT25", (await ReadAsync<ErrorResponse>(response)).Id);
    }
}
