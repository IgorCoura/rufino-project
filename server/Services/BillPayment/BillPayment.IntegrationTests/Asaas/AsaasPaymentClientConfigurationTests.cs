namespace BillPayment.IntegrationTests.Asaas;

using BillPayment.Infra;
using BillPayment.Infra.Asaas;
using BillPayment.Infra.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O que os clientes HTTP de PAGAMENTO carregam — e sobretudo o que eles NÃO carregam.
/// </summary>
/// <remarks>
/// Molde do <see cref="AsaasClientConfigurationTests"/>: monta o contêiner de DI porque os
/// testes de tradução constroem o transporte à mão e não enxergam nada do <c>AddHttpClient</c>.
/// A regra sob teste aqui vale dinheiro: <strong>o cliente de pagamento não pode ter handler de
/// resiliência</strong> — uma retentativa automática numa submissão é candidata a pagamento
/// duplicado, e a retentativa correta é a da fila, que confere por <c>externalReference</c>.
/// </remarks>
public sealed class AsaasPaymentClientConfigurationTests
{
    private const string BaseUrl = "https://api-sandbox.asaas.com/v3";

    // O cliente de pagamento sai configurado (base + User-Agent) e SEM retry automático.
    [Fact]
    public void PaymentClient_ShouldBeConfiguredWithoutAResilienceHandler()
    {
        using var provider = BuildProvider();

        using var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(AsaasHttp.PAYMENT_CLIENT_NAME);
        Assert.Equal(BaseUrl + "/", client.BaseAddress?.ToString());
        Assert.Equal(AsaasOptions.USER_AGENT, client.DefaultRequestHeaders.UserAgent.ToString());

        Assert.DoesNotContain(
            HandlerChainOf(provider, AsaasHttp.PAYMENT_CLIENT_NAME),
            name => name.Contains("Resilience", StringComparison.OrdinalIgnoreCase));
    }

    // O cliente do comprovante idem: sem retry (a retentativa é a reentrega do outbox) e sem
    // BaseAddress — a URL do comprovante é absoluta, uma capability URL do provedor.
    [Fact]
    public void ReceiptClient_ShouldBeConfiguredWithoutAResilienceHandlerAndWithoutABaseAddress()
    {
        using var provider = BuildProvider();

        using var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(HttpPaymentReceiptFetcher.CLIENT_NAME);
        Assert.Null(client.BaseAddress);
        Assert.Equal(AsaasOptions.USER_AGENT, client.DefaultRequestHeaders.UserAgent.ToString());

        Assert.DoesNotContain(
            HandlerChainOf(provider, HttpPaymentReceiptFetcher.CLIENT_NAME),
            name => name.Contains("Resilience", StringComparison.OrdinalIgnoreCase));
    }

    // A CONTRAPROVA: o cliente de consulta TEM o handler de resiliência. Sem ela, uma mudança
    // que arrancasse a resiliência de todos os clientes deixaria os dois testes acima verdes.
    [Fact]
    public void LookupClient_ShouldKeepItsResilienceHandler()
    {
        using var provider = BuildProvider();

        Assert.Contains(
            HandlerChainOf(provider, AsaasHttp.LOOKUP_CLIENT_NAME),
            name => name.Contains("Resilience", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Os nomes dos handlers na cadeia do cliente nomeado, do mais externo ao transporte.</summary>
    private static List<string> HandlerChainOf(ServiceProvider provider, string clientName)
    {
        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(clientName);

        var chain = new List<string>();
        for (var current = handler; current is not null; current = (current as DelegatingHandler)?.InnerHandler)
            chain.Add(current.GetType().Name);

        return chain;
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
