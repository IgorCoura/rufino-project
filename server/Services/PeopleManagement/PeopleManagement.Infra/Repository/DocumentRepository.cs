using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class DocumentRepository(PeopleManagementContext context) : Repository<Document>(context), IDocumentRepository
    {
        public async Task<List<DocumentStatus>> GetAllStatusByEmployeeAsync(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            return await context.Documents
                .Where(x => x.EmployeeId == employeeId && x.CompanyId == companyId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Status)
                .ToListAsync(cancellationToken);
        }
    }
}
