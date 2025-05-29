namespace PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Interfaces
{
    public interface IWorkplaceRepository
    {
        Task<Workplace?> GetWorkplaceFromEmployeeId(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
    }
}
