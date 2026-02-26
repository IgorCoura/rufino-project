using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Domain.Services
{
    public interface IEmployeeDocumentStatusService
    {
        EmployeeDocumentStatus DetermineStatusFromDocumentStatuses(List<DocumentStatus> documentStatuses);
    }
}
