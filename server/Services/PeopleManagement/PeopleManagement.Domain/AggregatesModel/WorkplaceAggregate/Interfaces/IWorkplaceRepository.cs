﻿namespace PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Interfaces
{
    public interface IWorkplaceRepository : IRepository<Workplace>
    {
        Task<Workplace?> GetWorkplaceFromEmployeeId(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
    }
}
