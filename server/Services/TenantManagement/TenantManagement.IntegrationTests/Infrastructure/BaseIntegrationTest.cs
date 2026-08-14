namespace TenantManagement.IntegrationTests.Infrastructure;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TenantManagement.Domain.Tenants;
using TenantManagement.Infra.Persistence;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected IntegrationTestWebAppFactory Factory { get; } = factory;

    protected RecordingTenantAccessProvisioner Provisioner => Factory.Provisioner;

    public Task InitializeAsync() => Task.CompletedTask;

    // O reset vai no fim: cada teste começa limpo e semeia o seu próprio estado.
    public Task DisposeAsync() => Factory.ResetDatabaseAsync();

    /// <summary>
    /// Cliente do back-office: autenticado, sem tenant no header — é assim que um operador da
    /// plataforma chega, e é o que prova que o guard de rota não o tranca para fora.
    /// </summary>
    protected HttpClient CreateAdminClient(Guid? requestId = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "test-token");
        client.DefaultRequestHeaders.Add("x-requestid", (requestId ?? Guid.NewGuid()).ToString());
        return client;
    }

    /// <summary>Cliente de uma pessoa: e-mail no token e a lista de tenants que ela acessa.</summary>
    protected HttpClient CreateMemberClient(string email, params Guid[] tenants)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "test-token");
        client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(MockAuthenticationHandler.UserEmailHeader, email);

        if (tenants.Length > 0)
            client.DefaultRequestHeaders.Add("tenants", string.Join(",", tenants));

        return client;
    }

    /// <summary>Lê o tenant em um scope novo: o do request devolveria o objeto do change tracker.</summary>
    protected Task<Tenant?> GetTenantAsync(Guid id)
    {
        var tenantId = TenantId.From(id);

        return Factory.ExecuteDbContextAsync(context => context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId));
    }

    /// <summary>
    /// Desserializa a resposta e, quando ela não é o que se esperava, joga o <strong>corpo
    /// cru</strong> na mensagem da falha. Sem isto, um 500 vira "'S' is an invalid start of a
    /// value" e esconde a exceção que o servidor devolveu — que é justamente o que se precisa ler.
    /// </summary>
    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var body = await response.Content.ReadAsStringAsync();

        try
        {
            return JsonSerializer.Deserialize<T>(body, Json)
                ?? throw new InvalidOperationException($"Resposta vazia ao desserializar {typeof(T).Name}.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Resposta {(int)response.StatusCode} não é um {typeof(T).Name}. Corpo: {body}", ex);
        }
    }
}
