namespace BillPayment.IntegrationTests.PayerProfiles;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Ports;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A chave da subconta Asaas do tenant: provada contra o provedor, guardada CIFRADA no cofre,
/// com só o ponteiro no perfil — e nunca devolvida pela API. A prova é determinística
/// (<see cref="FakePaymentAccountVerifier"/>); o fluxo prova → cofre → perfil é o real.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class AsaasAccountLinkTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");

    private const string Cnpj = "11222333000181";
    private const string ApiKey = "$aact_prod_chave_da_subconta_do_tenant_01";

    private readonly WebApplicationFactory<Program> _host;
    private readonly FakePaymentAccountVerifier _verifier;
    private readonly HttpClient _client;

    public AsaasAccountLinkTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithFakePaymentVerifier();
        _verifier = _host.Services.GetRequiredService<FakePaymentAccountVerifier>();
        _client = _host.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _verifier.Reset();
        _client.Dispose();
        _host.Dispose();
    }

    private static Uri Route() => new($"/api/v1/{TenantId}/payer-profile", UriKind.Relative);

    private static Uri AccountRoute() => new($"/api/v1/{TenantId}/payer-profile/asaas-account", UriKind.Relative);

    // O caminho feliz: a chave crua chega à prova, entra cifrada no cofre, o perfil guarda o
    // ponteiro bpv1: e o agendamento é liberado.
    [Fact]
    public async Task PutAsaasAccount_WithAValidKey_ShouldProveStoreAndLink()
    {
        await RegisterProfileAsync();

        var response = await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LinkAsaasAccountResponseContract>();
        Assert.True(body!.CanSchedulePayments);
        Assert.Equal(ApiKey, _verifier.LastApiKey);

        var profile = await LoadProfileAsync();
        Assert.NotNull(profile.AsaasAccountRef);
        Assert.True(profile.AsaasAccountRef!.IsLocalVault);

        var secrets = await CountTenantSecretsAsync();
        Assert.Equal(1, secrets);
    }

    // Prova recusada: 409 BLP.PRF12, e NADA fica para trás — nem ponteiro no perfil, nem
    // segredo órfão no cofre (a unidade de trabalho inteira é descartada).
    [Fact]
    public async Task PutAsaasAccount_WhenTheProviderRejectsTheKey_ShouldLeaveNothingBehind()
    {
        await RegisterProfileAsync();
        _verifier.NextProbe = PaymentAccountProbe.Rejected("invalid_api_key");

        var response = await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PRF12", error!.Id);

        var profile = await LoadProfileAsync();
        Assert.Null(profile.AsaasAccountRef);
        Assert.Equal(0, await CountTenantSecretsAsync());
    }

    // Provedor fora do ar não é chave errada: 409 BLP.PRF13, retentável por quem configura.
    [Fact]
    public async Task PutAsaasAccount_WhenTheProviderIsUnreachable_ShouldSayItIsNotTheKey()
    {
        await RegisterProfileAsync();
        _verifier.NextProbe = PaymentAccountProbe.Unavailable("transport_error");

        var response = await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PRF13", error!.Id);
    }

    // Trocar a chave remove o segredo antigo do cofre — uma linha por tenant, sempre.
    [Fact]
    public async Task PutAsaasAccount_Twice_ShouldReplaceTheSecretInsteadOfAccumulating()
    {
        await RegisterProfileAsync();
        await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));
        var first = (await LoadProfileAsync()).AsaasAccountRef;

        var response = await _client.PutAsJsonAsync(
            AccountRoute(), new LinkAsaasAccountRequest("$aact_prod_chave_nova_02"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await LoadProfileAsync();
        Assert.NotEqual(first, profile.AsaasAccountRef);
        Assert.Equal(1, await CountTenantSecretsAsync());
    }

    // Desvincular limpa o ponteiro e o cofre; repetir é inócuo.
    [Fact]
    public async Task DeleteAsaasAccount_ShouldClearTheLinkAndTheVault()
    {
        await RegisterProfileAsync();
        await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));

        var response = await _client.DeleteAsync(AccountRoute());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null((await LoadProfileAsync()).AsaasAccountRef);
        Assert.Equal(0, await CountTenantSecretsAsync());

        var again = await _client.DeleteAsync(AccountRoute());
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    // Chave vazia é omissão do campo, não desvínculo: 400 BLP.PRF11.
    [Fact]
    public async Task PutAsaasAccount_WithABlankKey_ShouldReturnBadRequest()
    {
        await RegisterProfileAsync();

        var response = await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PRF11", error!.Id);
    }

    // A chave nunca volta pela API: nem a crua, nem o ponteiro do cofre.
    [Fact]
    public async Task GetPayerProfile_ShouldExposeNeitherTheKeyNorThePointer()
    {
        await RegisterProfileAsync();
        await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));
        var pointer = (await LoadProfileAsync()).AsaasAccountRef!.ToString();

        var response = await _client.GetAsync(Route());
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(ApiKey, raw, StringComparison.Ordinal);
        Assert.DoesNotContain(pointer, raw, StringComparison.Ordinal);
        Assert.Contains("\"canSchedulePayments\":true", raw, StringComparison.Ordinal);
    }

    private async Task RegisterProfileAsync()
    {
        var response = await _client.PostAsJsonAsync(
            Route(), new RegisterPayerProfileRequest("Company", "RUFINO", Cnpj));
        response.EnsureSuccessStatusCode();
    }

    private Task<Domain.PayerProfiles.PayerProfile> LoadProfileAsync()
        => ExecuteDbContextAsync(db => db.PayerProfiles
            .AsNoTracking()
            .SingleAsync(p => p.TenantId == Domain.SharedKernel.TenantId.From(TenantId)));

    private Task<int> CountTenantSecretsAsync()
        => ExecuteDbContextAsync(db => db.TenantSecrets.AsNoTracking().CountAsync());
}
