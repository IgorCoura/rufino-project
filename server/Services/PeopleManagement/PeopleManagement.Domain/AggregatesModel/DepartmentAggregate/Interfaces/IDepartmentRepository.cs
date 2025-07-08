namespace PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces
{
    public interface IDepartmentRepository : IRepository<Department>
    {
        Task<Department?> GetDepartmentFromEmployeeId(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
    }
}
