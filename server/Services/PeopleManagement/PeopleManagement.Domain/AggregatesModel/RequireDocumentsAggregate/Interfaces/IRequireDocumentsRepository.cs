namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces
{
    public interface IRequireDocumentsRepository : IRepository<RequireDocuments>
    {
        Task<IEnumerable<RequireDocuments>> GetAllWithEventId(Guid employeeId, Guid companyId, int eventId, CancellationToken cancellationToken = default);
    }
}
