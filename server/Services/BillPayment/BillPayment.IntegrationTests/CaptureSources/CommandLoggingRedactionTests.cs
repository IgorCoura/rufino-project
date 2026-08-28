namespace BillPayment.IntegrationTests.CaptureSources;

using System.Net.Http.Json;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// O <c>BaseController</c> loga todo Command antes e depois do despacho. Estes testes provam que
/// esse log NÃO carrega segredo — a garantia do ADR-009 no único lugar onde ela some sem quebrar
/// build nem teste: dentro de uma string já formatada.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CommandLoggingRedactionTests : BaseIntegrationTest
{
    private static readonly Guid Tenant = new("0195a1f0-0000-7000-8000-00000000009a");

    private const string CredencialDoRegistroDeApp = "segredo-do-registro-de-app-nao-pode-vazar";

    private readonly RecordingLoggerProvider _logs = new();
    private readonly HttpClient _client;

    public CommandLoggingRedactionTests(IntegrationTestWebAppFactory factory) : base(factory, Tenant)
        => _client = factory.WithReachableMailbox()
            .WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddProvider(_logs)))
            .CreateClient().Authenticated(Tenant);

    // A credencial mandada para POST /capture-sources não aparece em nenhuma linha de log.
    [Fact]
    public async Task ConnectCaptureSource_ShouldNeverWriteTheCredentialToTheLog()
    {
        var payload = new ConnectCaptureSourceRequest(
            "MicrosoftGraphMailbox", "Caixa de contas a pagar", "contas@empresa.com.br", CredencialDoRegistroDeApp);

        await _client.PostAsJsonAsync(new Uri($"/api/v1/{Tenant}/capture-sources", UriKind.Relative), payload);

        Assert.False(
            _logs.AnyContains(CredencialDoRegistroDeApp),
            "A credencial do registro de app apareceu no log — ADR-009 violado.");
    }

    // O Command sensível ainda é logado: sai o nome e o marcador de omissão, não o silêncio.
    [Fact]
    public async Task ConnectCaptureSource_ShouldStillLogTheCommandNameAndTheRedactionMarker()
    {
        var payload = new ConnectCaptureSourceRequest(
            "MicrosoftGraphMailbox", "Caixa de contas a pagar", "contas@empresa.com.br", CredencialDoRegistroDeApp);

        await _client.PostAsJsonAsync(new Uri($"/api/v1/{Tenant}/capture-sources", UriKind.Relative), payload);

        Assert.True(_logs.AnyContains("ConnectCaptureSourceCommand"), "O nome do Command não foi logado.");
        Assert.True(_logs.AnyContains("[omitido: ISensitiveCommand]"), "O marcador de omissão não foi logado.");
    }

    // O CPF/CNPJ do cadastro do pagador não aparece no log: é dele que a senha de PDF é derivada
    // (ADR-009), então o comando que o carrega é sensível — mesmo sem ser segredo forte.
    [Fact]
    public async Task RegisterPayerProfile_ShouldNeverWriteTheTaxIdToTheLog()
    {
        const string cnpj = "11.222.333/0001-81";

        var payload = new RegisterPayerProfileRequest("Company", "Empresa Pagadora Ltda", cnpj);

        await _client.PostAsJsonAsync(new Uri($"/api/v1/{Tenant}/payer-profile", UriKind.Relative), payload);

        Assert.True(_logs.AnyContains("RegisterPayerProfileCommand"), "O nome do Command não foi logado.");
        Assert.False(_logs.AnyContains(cnpj), "O CNPJ formatado apareceu no log.");
        Assert.False(_logs.AnyContains("11222333000181"), "O CNPJ apareceu no log.");
    }

    // Contraprova: Command SEM o marcador tem o payload logado — sem ela, um logger quebrado
    // faria os dois testes acima passarem à toa.
    [Fact]
    public async Task RegisterTrustedOrigin_WhenCommandIsNotSensitive_ShouldLogThePayload()
    {
        var payload = new RegisterTrustedOriginRequest(
            "EmailAddress", "financeiro@fornecedor.com.br", "Trusted", null);

        await _client.PostAsJsonAsync(new Uri($"/api/v1/{Tenant}/trusted-origins", UriKind.Relative), payload);

        Assert.True(_logs.AnyContains("RegisterTrustedOriginCommand"), "O nome do Command não foi logado.");
        Assert.True(
            _logs.AnyContains("financeiro@fornecedor.com.br"),
            "O payload de um Command não sensível deveria ter sido logado.");
    }
}
