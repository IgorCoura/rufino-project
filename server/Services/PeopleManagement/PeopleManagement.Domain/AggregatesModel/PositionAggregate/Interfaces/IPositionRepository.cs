namespace PeopleManagement.Domain.AggregatesModel.PositionAggregate.Interfaces
{
    public interface IPositionRepository : IRepository<Position>
    {
        Task<Position?> GetPositionFromEmployeeId(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
    }
}
