namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role?> GetRoleFromEmployeeId(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
    }
}
