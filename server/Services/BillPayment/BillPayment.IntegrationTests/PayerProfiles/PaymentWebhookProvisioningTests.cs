namespace BillPayment.IntegrationTests.PayerProfiles;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Application.PayerProfiles.Commands;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Infra.Outbox;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O webhook do tenant: provisionado ao vincular a chave, conferido no provedor pela varredura,
/// consertado sem trocar o token e removido junto com a conta.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Esta classe existe por causa de um incidente.</strong> Entre 2026-09-08 e 2026-09-09 o
/// <c>AsaasAccountLinkedDomainEvent</c> era descartado antes de chegar ao outbox — o
/// <c>PayerProfile</c> não estava na lista de agregados do dreno do <c>SaveEntitiesAsync</c> —,
/// e nenhum webhook foi provisionado, sem um único erro em log nenhum. Não havia teste que
/// cobrisse o caminho: as suítes paravam no cofre e no ponteiro do perfil.
/// </para>
/// <para>
/// O primeiro teste daqui é o que teria falhado.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class PaymentWebhookProvisioningTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid TenantId = TestTenants.Primary;

    private const string Cnpj = "11222333000181";
    private const string ApiKey = "$aact_prod_chave_da_subconta_do_tenant_01";

    private readonly WebApplicationFactory<Program> _host;
    private readonly FakePaymentAccountVerifier _verifier;
    private readonly FakePaymentWebhookProvisioner _provisioner;
    private readonly HttpClient _client;

    public PaymentWebhookProvisioningTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithFakeWebhookProvisioner();
        _verifier = _host.Services.GetRequiredService<FakePaymentAccountVerifier>();
        _provisioner = _host.Services.GetRequiredService<FakePaymentWebhookProvisioner>();
        _client = _host.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _verifier.Reset();
        _provisioner.Reset();
        _client.Dispose();
        _host.Dispose();
    }

    private static Uri Route() => new($"/api/v1/{TenantId}/payer-profile", UriKind.Relative);

    private static Uri AccountRoute() => new($"{Route()}/asaas-account", UriKind.Relative);

    private static Uri WebhookRoute() => new($"{Route()}/asaas-webhook", UriKind.Relative);

    private static string ExpectedCallbackUrl()
        => $"{IntegrationTestWebAppFactory.TestWebhookBaseUrl}/webhooks/asaas/{TenantId}";

    // O TESTE DO INCIDENTE. Vincular a chave tem de deixar uma mensagem no outbox — se o evento
    // não é drenado, ele morre no fim do escopo e o webhook nunca existe, em silêncio.
    [Fact]
    public async Task LinkingTheAccount_ShouldEnqueueTheProvisioningEventInTheOutbox()
    {
        await RegisterProfileAsync();

        var response = await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var queued = await ExecuteDbContextAsync(db => db.OutboxMessages
            .AsNoTracking()
            .CountAsync(m => m.EventType == typeof(AsaasAccountLinkedDomainEvent).FullName));

        Assert.Equal(1, queued);
    }

    // E o outbox drenado tem de chegar ao provedor com a URL desta instalação e um token NOVO.
    [Fact]
    public async Task DrainingTheOutbox_ShouldProvisionTheWebhookWithAFreshToken()
    {
        await LinkAccountAsync();

        var call = Assert.Single(_provisioner.EnsureCalls);
        Assert.Equal(ExpectedCallbackUrl(), call.CallbackUrl);

        // Token nulo é o pedido de rotação — chave nova é conta possivelmente nova, e o token da
        // anterior não vale para ela.
        Assert.Null(call.AuthToken);

        var profile = await LoadProfileAsync();
        Assert.True(profile.HasWebhook);
        Assert.NotNull(profile.AsaasWebhookRotatedAt);
    }

    // Sem URL pública não há webhook possível, e isso não pode explodir: sai por Skipped e a
    // instalação segue dependendo da conciliação.
    [Fact]
    public async Task WithoutAPublicBaseUrl_ProvisioningShouldSkipInsteadOfFailing()
    {
        using var host = Factory.WithFakeWebhookProvisioner(publicBaseUrl: string.Empty);

        var provisioner = host.Services.GetRequiredService<FakePaymentWebhookProvisioner>();
        using var client = host.CreateClient().Authenticated();

        await client.PostAsJsonAsync(Route(), new RegisterPayerProfileRequest("Company", "RUFINO", Cnpj));
        var response = await client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await DrainOutboxAsync(host);

        Assert.Empty(provisioner.EnsureCalls);
        Assert.False((await LoadProfileAsync()).HasWebhook);
    }

    // A VARREDURA, caso número um: o outbox esgotou o backoff e ninguém provisionou. Ela acha o
    // tenant com conta e sem webhook, e conserta.
    [Fact]
    public async Task Sweeping_WhenTheOutboxNeverProvisioned_ShouldProvisionFromScratch()
    {
        await RegisterProfileAsync();
        await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));

        // O outbox NÃO é drenado: é exatamente o cenário do provedor fora do ar durante as cinco
        // tentativas. Sem varredura, este tenant ficaria sem webhook para sempre.
        Assert.False((await LoadProfileAsync()).HasWebhook);

        var result = await SweepAsync();

        Assert.Equal(SweepPaymentWebhookCommandHandler.OUTCOME_PROVISIONED, result.Outcome);
        Assert.True((await LoadProfileAsync()).HasWebhook);
    }

    // Íntegro no provedor: a varredura não toca em nada. É o desfecho comum, e o que garante que
    // ela não vira uma máquina de rotacionar token de meia em meia hora.
    [Fact]
    public async Task Sweeping_WhenTheWebhookIsDelivering_ShouldDoNothing()
    {
        await LinkAccountAsync();
        _provisioner.EnsureCalls.Clear();
        _provisioner.NextHealth = WebhookHealthResult.Healthy(
            enabled: true, interrupted: false, penalizedRequestsCount: 0, url: ExpectedCallbackUrl());

        var result = await SweepAsync();

        Assert.Equal(SweepPaymentWebhookCommandHandler.OUTCOME_HEALTHY, result.Outcome);
        Assert.Empty(_provisioner.EnsureCalls);
    }

    // Fila pausada pelo provedor (15 falhas consecutivas): conserta com o MESMO token. Rotacionar
    // aqui seria trocar o segredo a cada conserto, e cada troca é uma janela de webhook mudo.
    [Fact]
    public async Task Sweeping_WhenTheQueueIsInterrupted_ShouldHealWithoutRotatingTheToken()
    {
        await LinkAccountAsync();
        var rotatedBefore = (await LoadProfileAsync()).AsaasWebhookRotatedAt;
        _provisioner.EnsureCalls.Clear();
        _provisioner.NextHealth = WebhookHealthResult.Healthy(
            enabled: true, interrupted: true, penalizedRequestsCount: 15, url: ExpectedCallbackUrl());

        var result = await SweepAsync();

        Assert.Equal(SweepPaymentWebhookCommandHandler.OUTCOME_HEALED, result.Outcome);

        var call = Assert.Single(_provisioner.EnsureCalls);
        Assert.NotNull(call.AuthToken);
        Assert.Equal(rotatedBefore, (await LoadProfileAsync()).AsaasWebhookRotatedAt);
    }

    // URL antiga no provedor (mudou o domínio da API): mesmo conserto, mesmo token.
    [Fact]
    public async Task Sweeping_WhenTheProviderPointsElsewhere_ShouldHealTheUrl()
    {
        await LinkAccountAsync();
        _provisioner.EnsureCalls.Clear();
        _provisioner.NextHealth = WebhookHealthResult.Healthy(
            enabled: true, interrupted: false, penalizedRequestsCount: 0,
            url: "https://dominio-antigo.test/webhooks/asaas/" + TenantId);

        var result = await SweepAsync();

        Assert.Equal(SweepPaymentWebhookCommandHandler.OUTCOME_HEALED, result.Outcome);
        Assert.Equal(ExpectedCallbackUrl(), Assert.Single(_provisioner.EnsureCalls).CallbackUrl);
    }

    // Apagado no painel do provedor: o token do cofre já não vale para webhook nenhum, então
    // recriar é rotacionar.
    [Fact]
    public async Task Sweeping_WhenTheWebhookVanishedAtTheProvider_ShouldRecreateWithAFreshToken()
    {
        await LinkAccountAsync();
        _provisioner.EnsureCalls.Clear();
        _provisioner.NextHealth = WebhookHealthResult.NotFound();

        var result = await SweepAsync();

        Assert.Equal(SweepPaymentWebhookCommandHandler.OUTCOME_RECREATED, result.Outcome);
        Assert.Null(Assert.Single(_provisioner.EnsureCalls).AuthToken);
    }

    // Provedor fora do ar NÃO vira conserto: recriar por não conseguir ler deixaria um webhook
    // órfão na conta do tenant a cada instabilidade.
    [Fact]
    public async Task Sweeping_WhenTheProviderIsUnreachable_ShouldChangeNothing()
    {
        await LinkAccountAsync();
        _provisioner.EnsureCalls.Clear();
        _provisioner.NextHealth = WebhookHealthResult.Unavailable("transport_error");

        var result = await SweepAsync();

        Assert.Equal(SweepPaymentWebhookCommandHandler.OUTCOME_UNAVAILABLE, result.Outcome);
        Assert.Empty(_provisioner.EnsureCalls);
    }

    // Desvincular a conta derruba o webhook no provedor e não deixa segredo órfão no cofre.
    // Antes de 2026-09-09 o webhook continuava vivo apontando para cá e o token ficava no cofre.
    [Fact]
    public async Task UnlinkingTheAccount_ShouldRemoveTheWebhookAtTheProviderAndTheSecrets()
    {
        await LinkAccountAsync();
        var webhookId = (await LoadProfileAsync()).AsaasWebhookId;

        var response = await _client.DeleteAsync(AccountRoute());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(webhookId, Assert.Single(_provisioner.RemovedWebhookIds));

        var profile = await LoadProfileAsync();
        Assert.False(profile.HasWebhook);
        Assert.Null(profile.AsaasAccountRef);
        Assert.Null(profile.AsaasWebhookRotatedAt);
        Assert.Equal(0, await ExecuteDbContextAsync(db => db.TenantSecrets.AsNoTracking().CountAsync()));
    }

    // A tela de diagnóstico: diz se está entregando, e não vaza segredo nenhum.
    [Fact]
    public async Task InspectingTheWebhook_ShouldReportDeliveryWithoutLeakingTheToken()
    {
        await LinkAccountAsync();
        _provisioner.NextHealth = WebhookHealthResult.Healthy(
            enabled: true, interrupted: false, penalizedRequestsCount: 2, url: ExpectedCallbackUrl());

        var response = await _client.GetAsync(WebhookRoute());
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"delivering\":true", raw, StringComparison.Ordinal);
        Assert.Contains("\"penalizedRequestsCount\":2", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("tok_", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("bpv1", raw, StringComparison.Ordinal);
    }

    // A alavanca manual: reprovisiona com token novo mesmo estando tudo íntegro — é o único
    // conserto possível quando o cofre e o provedor saíram de sincronia, defeito que a varredura
    // não consegue DIAGNOSTICAR porque o provedor nunca devolve o token.
    [Fact]
    public async Task Reprovisioning_ShouldAlwaysRotateTheToken()
    {
        await LinkAccountAsync();
        var rotatedBefore = (await LoadProfileAsync()).AsaasWebhookRotatedAt;
        _provisioner.EnsureCalls.Clear();

        var response = await _client.PostAsync(
            new Uri($"{WebhookRoute()}/reprovision", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(Assert.Single(_provisioner.EnsureCalls).AuthToken);
        Assert.True((await LoadProfileAsync()).AsaasWebhookRotatedAt > rotatedBefore);
    }

    private async Task LinkAccountAsync()
    {
        await RegisterProfileAsync();
        var response = await _client.PutAsJsonAsync(AccountRoute(), new LinkAsaasAccountRequest(ApiKey));
        response.EnsureSuccessStatusCode();

        await DrainOutboxAsync(_host);
    }

    private async Task RegisterProfileAsync()
    {
        var response = await _client.PostAsJsonAsync(
            Route(), new RegisterPayerProfileRequest("Company", "RUFINO", Cnpj));
        response.EnsureSuccessStatusCode();
    }

    private async Task<SweepPaymentWebhookResponse> SweepAsync()
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Application.Mediator.IMediator>();

        return await mediator.Send(new SweepPaymentWebhookCommand(TenantId), CancellationToken.None);
    }

    private static async Task DrainOutboxAsync(WebApplicationFactory<Program> host)
    {
        using var scope = host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        while (await processor.ProcessPendingAsync(CancellationToken.None) > 0)
        {
        }
    }

    private Task<PayerProfile> LoadProfileAsync()
        => ExecuteDbContextAsync(db => db.PayerProfiles
            .AsNoTracking()
            .SingleAsync(p => p.TenantId == Domain.SharedKernel.TenantId.From(TenantId)));
}
