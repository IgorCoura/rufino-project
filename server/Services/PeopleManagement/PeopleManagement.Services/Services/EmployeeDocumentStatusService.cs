using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.Services
{
    public class EmployeeDocumentStatusService : IEmployeeDocumentStatusService
    {
        public EmployeeDocumentStatus DetermineStatusFromDocumentStatuses(List<DocumentStatus> documentStatuses)
            => DocumentStatusRollup.Summarize(documentStatuses);
    }
}
