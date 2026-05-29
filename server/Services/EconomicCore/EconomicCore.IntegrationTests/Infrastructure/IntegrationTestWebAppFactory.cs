namespace EconomicCore.IntegrationTests.Infrastructure;

using EconomicCore.Infra.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("economic_core_tests")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private Respawner _respawner = default!;
    private string _connectionString = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:EconomicCore", _connectionString);
        // Disable the polling worker in tests so the outbox is driven deterministically via IOutboxProcessor.
        builder.UseSetting("Outbox:Enabled", "false");
        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        var options = new DbContextOptionsBuilder<EconomicCoreDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        await using var context = new EconomicCoreDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["economic_core"],
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async Task ExecuteDbContextAsync(Func<EconomicCoreDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EconomicCoreDbContext>();
        await action(context);
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<EconomicCoreDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EconomicCoreDbContext>();
        return await action(context);
    }
}
