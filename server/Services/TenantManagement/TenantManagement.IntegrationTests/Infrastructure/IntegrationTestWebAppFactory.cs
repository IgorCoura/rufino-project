namespace TenantManagement.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using TenantManagement.Domain.Ports;
using TenantManagement.Infra;
using TenantManagement.Infra.Persistence;
using Testcontainers.PostgreSql;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("tenant_management_tests")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private Respawner _respawner = default!;
    private string _connectionString = default!;

    /// <summary>O provedor de identidade da suíte. Programável por teste; zerado entre eles.</summary>
    public RecordingTenantAccessProvisioner Provisioner { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{InfraDependencies.CONNECTION_STRING_NAME}", _connectionString);

        // O provisionamento fica desligado na configuração: quem responde pela porta é o dublê
        // registrado abaixo. Ligá-lo faria a suíte tentar falar com um Keycloak de verdade.
        builder.UseSetting("TenantProvisioning:Enabled", "false");

        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITenantAccessProvisioner>();
            services.AddSingleton<ITenantAccessProvisioner>(Provisioner);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = MockAuthenticationHandler.AuthScheme;
                options.DefaultChallengeScheme = MockAuthenticationHandler.AuthScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(
                MockAuthenticationHandler.AuthScheme, _ => { });

            services.AddSingleton<IAuthorizationHandler, MockAccessRequirementHandler>();
            services.RemoveAll<IAuthorizationPolicyProvider>();
            services.AddSingleton<IAuthorizationPolicyProvider, MockPolicyProvider>();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        // A tabela de histórico TEM que ser a mesma que a Infra configura. Este contexto é
        // construído à mão, fora do DI, então não herda o MigrationsHistoryTable do
        // AddInfraDependencies — e com o padrão do EF o histórico iria para outro schema: a
        // fábrica migraria, o host não acharia registro nenhum, tentaria criar tudo de novo e
        // morreria em 42P07 "relation already exists".
        var options = new DbContextOptionsBuilder<TenantManagementDbContext>()
            .UseNpgsql(_connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", TenantManagementDbContext.DEFAULT_SCHEMA))
            .Options;

        // Migrações, como em produção — a suíte valida o mesmo schema que o deploy produz.
        await using var context = new TenantManagementDbContext(options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = [TenantManagementDbContext.DEFAULT_SCHEMA],

            // O histórico de migrações NÃO é dado de teste: apagá-lo faz o próximo host subir
            // achando que o banco está vazio e tentar criar tudo outra vez.
            TablesToIgnore =
            [
                new Respawn.Graph.Table(TenantManagementDbContext.DEFAULT_SCHEMA, "__ef_migrations_history"),
            ],
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        Provisioner.Reset();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<TenantManagementDbContext, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();
        return await action(context);
    }
}
