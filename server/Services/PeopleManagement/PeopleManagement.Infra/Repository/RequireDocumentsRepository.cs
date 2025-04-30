using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class RequireDocumentsRepository(PeopleManagementContext context) : Repository<RequireDocuments>(context), IRequireDocumentsRepository
    {
        public async Task<IEnumerable<RequireDocuments>> GetAllWithEventId(Guid employeeId, Guid companyId, int eventId,
           CancellationToken cancellationToken = default)
        {
            var documentIds =  _context.Documents.Local
                .Where(d => d.EmployeeId == employeeId && d.CompanyId == companyId)
                .Select(d => d.RequiredDocumentId)
                .ToList();

            documentIds.AddRange(
                await _context.Documents
            .Where(d => d.EmployeeId == employeeId && d.CompanyId == companyId)
            .Select(d => d.RequiredDocumentId)
            .ToListAsync(cancellationToken)); 

            var query = _context.RequireDocuments
                .Where(rd => rd.CompanyId == companyId
                                && documentIds.Contains(rd.Id)
                                && rd.ListenEvents.Any(le => le.EventId == eventId));

            return await query.ToListAsync(cancellationToken);
           
        }
    }
}
