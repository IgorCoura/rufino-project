namespace PeopleManagement.IntegrationTests.Configs
{
    // Base comum a todas as classes de teste de integração. Recebe a factory compartilhada (collection fixture)
    // e, ao final de CADA teste, reseta o schema people_management via Respawn — garantindo isolamento
    // (sem poluição de estado entre os [Fact], que antes causava violações de unique constraint).
    public abstract class BaseIntegrationTest : IAsyncLifetime
    {
        protected readonly PeopleManagementWebApplicationFactory _factory;

        protected BaseIntegrationTest(PeopleManagementWebApplicationFactory factory)
        {
            _factory = factory;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();
    }
}
