using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class WorkplaceRepository(PeopleManagementContext context) : Repository<Workplace>(context), IWorkplaceRepository
    {
        public async Task<Workplace?> GetWorkplaceFromEmployeeId(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var query = from e in _context.Employees
                        join w in _context.Workplaces on e.WorkPlaceId equals w.Id
                        where e.Id == employeeId && e.CompanyId == companyId
                        select w;

            return await query.SingleOrDefaultAsync();
        }
    }
}
