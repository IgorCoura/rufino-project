namespace PeopleManagement.IntegrationTests.Configs
{
    // Uma única instância da factory (containers + host) compartilhada por toda a suíte.
    // Como todos os testes ficam na mesma collection, o xUnit os executa em série — pré-requisito
    // para o reset determinístico do banco via Respawn entre cada teste.
    [CollectionDefinition(nameof(IntegrationTestCollection))]
    public sealed class IntegrationTestCollection : ICollectionFixture<PeopleManagementWebApplicationFactory>
    {
    }
}
