namespace TenantManagement.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using TenantManagement.Domain.Tenants;
using TenantManagement.IntegrationTests.Contracts;
using TenantManagement.IntegrationTests.Infrastructure;
using TenantManagement.IntegrationTests.Mothers;

/// <summary>
/// O que suspender, reativar e habilitar produto fazem chegar ao provedor de identidade.
/// </summary>
/// <remarks>
/// É o canal de integração com os produtos: nenhum deles chama este BC em runtime (ADR-002), e
/// o token é a única coisa que atravessa. Portanto o que estes testes afirmam sobre o
/// provisionador é literalmente o que o BillPayment e o PeopleManagement vão enxergar.
/// </remarks>
public class TenantAccessLifecycleTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string SecondEmail = "socio@rufino.com.br";

    /// <remarks>
    /// <strong>Um cliente por ESCRITA, sempre.</strong> O <c>x-requestid</c> é fixado uma vez por
    /// <c>HttpClient</c>, então reaproveitá-lo faz a segunda escrita ser engolida como duplicata
    /// pelo <c>IdentifiedCommand</c> — e o teste falha dizendo que o efeito não aconteceu, quando
    /// o que aconteceu foi a idempotência funcionando.
    /// </remarks>
    private async Task<Guid> RegisterAsync()
        => (await ReadAsync<IdResponse>(
            await CreateAdminClient().PostAsJsonAsync("api/v1/tenants", TenantRequestMother.Company(), Json))).Id;

    private Task<HttpResponseMessage> SuspendAsync(Guid id)
        => CreateAdminClient().PostAsJsonAsync($"api/v1/tenants/{id}/suspend", new SuspendTenantRequest("Inadimplência"), Json);

    private Task<HttpResponseMessage> GrantMemberAsync(Guid id, string email)
        => CreateAdminClient().PostAsJsonAsync(
            $"api/v1/tenants/{id}/members", new GrantMembershipRequest(email, nameof(MembershipRole.Member)), Json);

    private Task<HttpResponseMessage> ActivateProductAsync(Guid id, ProductCode product)
        => CreateAdminClient().PostAsJsonAsync($"api/v1/tenants/{id}/products/{product.Name}", new { }, Json);

    // Suspender revoga o acesso de TODO vínculo ativo no provedor — o titular incluído.
    // Antes disto o evento era emitido e ninguém escutava: o tenant suspenso seguia operando
    // os dois produtos, contra o que o próprio TenantStatus.Suspended declara.
    [Fact]
    public async Task PostSuspend_ShouldRevokeAccessForEveryActiveMembership()
    {
        var id = await RegisterAsync();
        await GrantMemberAsync(id, SecondEmail);

        await SuspendAsync(id);

        var revokedEmails = Provisioner.Revoked.Select(r => r.Email).ToList();
        Assert.Contains(TenantRequestMother.OwnerEmail, revokedEmails);
        Assert.Contains(SecondEmail, revokedEmails);
    }

    // Suspender NÃO desfaz o cadastro: o vínculo continua ativo, e é por isso que a revogação
    // passa pelo provisionador em vez de RevokeMembership — o método de domínio protege o
    // último responsável (TNM.TNT20) e recusaria cortar justamente o dono.
    [Fact]
    public async Task PostSuspend_ShouldKeepMembershipsIntact()
    {
        var id = await RegisterAsync();

        await SuspendAsync(id);

        var tenant = await GetTenantAsync(id);
        var owner = Assert.Single(tenant!.Memberships);
        Assert.True(owner.IsActive);
        Assert.Equal(MembershipRole.Owner, owner.Role);
    }

    // Reativar devolve o acesso a quem tinha vínculo ativo.
    [Fact]
    public async Task PostReactivate_ShouldGrantAccessBack()
    {
        var id = await RegisterAsync();
        await SuspendAsync(id);
        Provisioner.Reset();

        await CreateAdminClient().PostAsJsonAsync($"api/v1/tenants/{id}/reactivate", new { }, Json);

        Assert.Contains(Provisioner.Granted, g => g.Email == TenantRequestMother.OwnerEmail);
    }

    // Habilitar um produto reescreve o acesso de quem já está no tenant, agora com o produto
    // na lista — é o que faz o token dizer em QUAIS produtos aquele tenant vale.
    [Fact]
    public async Task PostProduct_ShouldGrantAccessCarryingTheActiveProduct()
    {
        var id = await RegisterAsync();
        Provisioner.Reset();

        await ActivateProductAsync(id, ProductCode.BillPayment);

        var granted = Assert.Single(Provisioner.Granted);
        Assert.Equal(TenantRequestMother.OwnerEmail, granted.Email);
        Assert.Contains(ProductCode.BillPayment, granted.Products);
    }

    // Desabilitar um produto reescreve o acesso SEM ele e mantém os demais: a porta declara o
    // estado desejado, então o produto que sai da lista tem o acesso retirado no provedor.
    [Fact]
    public async Task DeleteProduct_ShouldGrantAccessWithoutTheDeactivatedProduct()
    {
        var id = await RegisterAsync();
        await ActivateProductAsync(id, ProductCode.BillPayment);
        await ActivateProductAsync(id, ProductCode.PeopleManagement);
        Provisioner.Reset();

        await CreateAdminClient().DeleteAsync($"api/v1/tenants/{id}/products/{nameof(ProductCode.BillPayment)}");

        var granted = Assert.Single(Provisioner.Granted);
        Assert.DoesNotContain(ProductCode.BillPayment, granted.Products);
        Assert.Contains(ProductCode.PeopleManagement, granted.Products);
    }

    // Conceder acesso a alguém leva os produtos que o tenant tem AGORA, lidos do agregado — o
    // payload do evento diria o que valia quando ele foi emitido.
    [Fact]
    public async Task PostMembers_ShouldGrantAccessCarryingTheCurrentProducts()
    {
        var id = await RegisterAsync();
        await ActivateProductAsync(id, ProductCode.BillPayment);
        Provisioner.Reset();

        await GrantMemberAsync(id, SecondEmail);

        var granted = Assert.Single(Provisioner.Granted, g => g.Email == SecondEmail);
        Assert.Contains(ProductCode.BillPayment, granted.Products);
    }

    // REGRESSÃO: reprovisionar um tenant SUSPENSO reemitia concessão para todo vínculo ativo, e
    // o endpoint de conserto virava a forma de burlar a suspensão — bastava pedir o
    // reprovisionamento para todo mundo voltar a entrar.
    [Fact]
    public async Task PostReprovision_WhenTenantIsSuspended_ShouldRevokeNotGrant()
    {
        var id = await RegisterAsync();
        Provisioner.FailRevocations = true;
        await SuspendAsync(id);
        Provisioner.Reset();

        await CreateAdminClient().PostAsJsonAsync($"api/v1/tenants/{id}/access/reprovision", new { }, Json);

        Assert.Empty(Provisioner.Granted);
        Assert.Contains(Provisioner.Revoked, r => r.Email == TenantRequestMother.OwnerEmail);
    }

    // Provedor fora do ar na suspensão não derruba a requisição: o tenant fica suspenso e o
    // vínculo marcado como falho, visível na consulta e curável pelo reprovisionamento.
    [Fact]
    public async Task PostSuspend_WhenProvisionerFails_ShouldSuspendAndMarkFailed()
    {
        var id = await RegisterAsync();
        Provisioner.FailRevocations = true;

        var response = await SuspendAsync(id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await GetTenantAsync(id);
        Assert.Equal(TenantStatus.Suspended, tenant!.Status);
        Assert.Equal(ProvisioningStatus.Failed, tenant.AccessProvisioning);
    }
}
