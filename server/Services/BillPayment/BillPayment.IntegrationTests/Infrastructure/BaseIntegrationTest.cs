namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Infra.Persistence;

/// <summary>
/// Os dois tenants canônicos da suíte. Quase toda classe de teste usa este par — o primeiro como
/// "o meu" e o segundo como "o do outro", que é o que sustenta os testes de isolamento.
/// </summary>
public static class TestTenants
{
    public static readonly Guid Primary = new("0195a1f0-0000-7000-8000-000000000001");
    public static readonly Guid Secondary = new("0195a1f0-0000-7000-8000-000000000002");

    public static readonly Guid[] Canonical = [Primary, Secondary];
}

public static class TestHttpClientExtensions
{
    /// <summary>
    /// Autentica o cliente e declara a quais tenants ele tem acesso.
    /// </summary>
    /// <remarks>
    /// O header <c>tenants</c> vira o claim de mesmo nome no dublê de autenticação, e é contra ele
    /// que o <c>RouteAccessRequirementHandler</c> <strong>de produção</strong> confere o
    /// <c>{tenantId}</c> da rota. Declarar os dois tenants canônicos não é atalho: é o cenário do
    /// ADR-008 — uma pessoa com acesso a duas contas —, e é o que mantém os testes de tenant
    /// cruzado provando o filtro do REPOSITÓRIO (404) em vez de pararem no guard (403).
    /// </remarks>
    public static HttpClient Authenticated(this HttpClient client, params Guid[] tenants)
    {
        ArgumentNullException.ThrowIfNull(client);

        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Remove(MockAuthenticationHandler.TenantsHeader);

        client.DefaultRequestHeaders.Add("Authorization", Guid.NewGuid().ToString());

        var declared = tenants is { Length: > 0 } ? tenants : TestTenants.Canonical;
        client.DefaultRequestHeaders.Add(
            MockAuthenticationHandler.TenantsHeader,
            string.Join(",", declared.Select(t => t.ToString())));

        return client;
    }
}

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected IntegrationTestWebAppFactory Factory { get; }

    protected HttpClient Client { get; }

    /// <param name="tenants">
    /// Tenants que o cliente desta classe alcança. Omitir usa o par canônico; classes com Guid
    /// próprio precisam declarar o seu, senão toda requisição volta 403 pelo guard de rota.
    /// </param>
    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory, params Guid[] tenants)
    {
        Factory = factory;
        Client = factory.CreateClient().Authenticated(tenants);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    protected async Task ExecuteDbContextAsync(Func<BillPaymentDbContext, Task> action)
    {
        await Factory.ExecuteDbContextAsync(action);
    }

    protected async Task<T> ExecuteDbContextAsync<T>(Func<BillPaymentDbContext, Task<T>> action)
    {
        return await Factory.ExecuteDbContextAsync(action);
    }
}
