namespace EconomicCore.IntegrationTests.Infrastructure;

using EconomicCore.Infra.Persistence;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected IntegrationTestWebAppFactory Factory { get; }
    protected HttpClient Client { get; }

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    protected async Task ExecuteDbContextAsync(Func<EconomicCoreDbContext, Task> action)
    {
        await Factory.ExecuteDbContextAsync(action);
    }

    protected async Task<T> ExecuteDbContextAsync<T>(Func<EconomicCoreDbContext, Task<T>> action)
    {
        return await Factory.ExecuteDbContextAsync(action);
    }
}
