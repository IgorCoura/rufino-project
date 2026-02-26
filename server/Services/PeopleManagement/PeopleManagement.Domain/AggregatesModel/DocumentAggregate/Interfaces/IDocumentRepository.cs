namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IDocumentRepository : IRepository<Document>
    {
        Task<List<DocumentStatus>> GetAllStatusByEmployeeAsync(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
    }
}
