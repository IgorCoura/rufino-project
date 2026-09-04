namespace BillPayment.IntegrationTests.Asaas;

using BillPayment.Domain.Ports;
using BillPayment.Infra;
using BillPayment.Infra.Asaas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O que o <c>HttpClient</c> do provedor carrega antes de a primeira requisição sair.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Monta o contêiner de DI de propósito, em vez de instanciar o adapter.</strong> Os
/// testes de tradução (<c>AsaasPixLookupServiceTests</c>, <c>AsaasBillLookupServiceTests</c>)
/// constroem o transporte à mão para exercitar o mapeamento da resposta — e por isso não
/// enxergam nada do que o <c>AddHttpClient</c> configura. O defeito de 2026-08-25 morava
/// exatamente nessa cegueira: faltava o <c>User-Agent</c> na configuração do cliente, as duas
/// suítes passavam, e toda chamada real voltava 400.
/// </para>
/// <para>
/// Sem contêiner nem containers: <c>AddInfraDependencies</c> registra o <c>DbContext</c> sem
/// abrir conexão, então este arquivo não precisa de Postgres e não entra na coleção
/// compartilhada.
/// </para>
/// </remarks>
public sealed class AsaasClientConfigurationTests
{
    private const string BaseUrl = "https://api-sandbox.asaas.com/v3";

    // Regressão: o Asaas recusa a requisição sem User-Agent ("É obrigatório preencher User-Agent
    // no cabeçalho da requisição") e o HttpClient do .NET não manda nenhum por padrão — o que
    // derrubava consulta de boleto e decode de Pix antes de o provedor olhar o corpo.
    [Fact]
    public void AsaasClient_ShouldSendAUserAgent()
    {
        using var provider = BuildProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(AsaasHttp.LOOKUP_CLIENT_NAME);

        // Prova que este é o cliente configurado, e não um homônimo vazio: sem isto, uma mudança
        // no nome do cliente nomeado faria o teste afirmar sobre outra coisa.
        Assert.Equal(BaseUrl + "/", client.BaseAddress?.ToString());

        Assert.Equal(AsaasOptions.USER_AGENT, client.DefaultRequestHeaders.UserAgent.ToString());
    }

    // A chave é POR TENANT (2026-08-31) e entra por chamada, resolvida do cofre: o cliente
    // compartilhado NÃO pode nascer com access_token — um header default aqui vazaria a chave
    // de um tenant para as chamadas de todos os outros.
    [Fact]
    public void AsaasClient_ShouldNotCarryADefaultAccessToken()
    {
        using var provider = BuildProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(AsaasHttp.LOOKUP_CLIENT_NAME);

        Assert.False(client.DefaultRequestHeaders.Contains("access_token"));
    }

    // O registro deixou de ser condicional: os adapters reais entram sempre, e "tenant sem
    // chave" é caso de dado (Unavailable na chamada), não de composição do contêiner.
    [Fact]
    public void LookupServices_ShouldAlwaysBeTheRealAdapters()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<AsaasBillLookupService>(scope.ServiceProvider.GetRequiredService<IBillLookupService>());
        Assert.IsType<AsaasPixLookupService>(scope.ServiceProvider.GetRequiredService<IPixLookupService>());
        Assert.IsType<AsaasAccountVerifier>(scope.ServiceProvider.GetRequiredService<IPaymentAccountVerifier>());
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BillPayment"] = "Host=localhost;Database=none",
                ["Asaas:BaseUrl"] = BaseUrl,
            })
            .Build();

        return new ServiceCollection()
            .AddInfraDependencies(configuration)
            .BuildServiceProvider();
    }
}
