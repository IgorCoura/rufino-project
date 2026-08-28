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
/// constroem o <c>HttpClient</c> à mão para exercitar o mapeamento da resposta — e por isso não
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
    private const string ApiKey = "$aact_chave_de_teste";
    private const string BaseUrl = "https://api-sandbox.asaas.com/v3";

    // Regressão: o Asaas recusa a requisição sem User-Agent ("É obrigatório preencher User-Agent
    // no cabeçalho da requisição") e o HttpClient do .NET não manda nenhum por padrão — o que
    // derrubava consulta de boleto e decode de Pix antes de o provedor olhar o corpo.
    [Theory]
    [InlineData(nameof(IBillLookupService))]
    [InlineData(nameof(IPixLookupService))]
    public void AsaasClient_ShouldSendAUserAgent(string clientName)
    {
        using var provider = BuildProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);

        // Prova que este é o cliente configurado, e não um homônimo vazio: sem isto, uma mudança
        // na derivação do nome do cliente tipado faria o teste afirmar sobre outra coisa.
        Assert.Equal(BaseUrl + "/", client.BaseAddress?.ToString());

        Assert.Equal(AsaasOptions.USER_AGENT, client.DefaultRequestHeaders.UserAgent.ToString());
    }

    // Sem chave não há adapter do provedor — entram os substitutos, e nenhum cliente HTTP é
    // configurado. É o estado em que a suíte inteira roda, e é o que torna o caso acima invisível.
    [Fact]
    public void WithoutAnApiKey_ShouldRegisterTheUnconfiguredSubstitutes()
    {
        using var provider = BuildProvider(apiKey: null);

        Assert.IsNotType<AsaasBillLookupService>(provider.GetRequiredService<IBillLookupService>());
        Assert.IsNotType<AsaasPixLookupService>(provider.GetRequiredService<IPixLookupService>());
    }

    private static ServiceProvider BuildProvider(string? apiKey = ApiKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BillPayment"] = "Host=localhost;Database=none",
                ["Asaas:BaseUrl"] = BaseUrl,
                ["Asaas:ApiKey"] = apiKey,
            })
            .Build();

        return new ServiceCollection()
            .AddInfraDependencies(configuration)
            .BuildServiceProvider();
    }
}
