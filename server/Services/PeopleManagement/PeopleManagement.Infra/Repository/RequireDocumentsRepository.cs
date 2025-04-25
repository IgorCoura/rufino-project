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
            var query = from requireDocument in _context.RequireDocuments
                        where requireDocument.CompanyId == companyId
                              && (from Document in _context.Documents
                                  where Document.EmployeeId == employeeId
                                        && Document.CompanyId == companyId
                                  select Document.RequiredDocumentId).ToArray().Contains(requireDocument.Id)
                              && ((string)(object)requireDocument.ListenEventsIds).Contains(eventId.ToString())
                        select requireDocument;

            return await query.ToListAsync(cancellationToken);
        }
    }
}
