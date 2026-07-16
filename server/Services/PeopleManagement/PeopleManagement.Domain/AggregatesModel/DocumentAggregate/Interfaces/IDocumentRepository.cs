namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IDocumentRepository : IRepository<Document>
    {
        Task<List<DocumentStatus>> GetAllStatusByEmployeeAsync(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);

        // Contador de renovações: cada ciclo de vencimento deprecia a unidade que venceu, então o número de
        // unidades Deprecated é quantas vezes o documento já renovou. Contado à parte para não carregar todas as
        // unidades no aggregate (o fluxo de depreciação carrega só a que venceu, e mudar isso alteraria o
        // MakeAsDeprecated do caminho de não-associação).
        Task<int> CountDeprecatedUnitsAsync(Guid documentId, Guid companyId, CancellationToken cancellationToken = default);
    }
}
