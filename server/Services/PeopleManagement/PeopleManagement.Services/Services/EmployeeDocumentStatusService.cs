using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.Services
{
    public class EmployeeDocumentStatusService : IEmployeeDocumentStatusService
    {
        public EmployeeDocumentStatus DetermineStatusFromDocumentStatuses(List<DocumentStatus> documentStatuses)
        {


            if (documentStatuses == null || documentStatuses.Count == 0)
            {
                return EmployeeDocumentStatus.Okay;
            }

            if (documentStatuses.Any(x => x == DocumentStatus.RequiresDocument || x == DocumentStatus.RequiresValidation))
            {
                return EmployeeDocumentStatus.RequiresAttention;
            }

            if (documentStatuses.Any(x => x == DocumentStatus.Warning))
            {
                return EmployeeDocumentStatus.Warning;
            }

            return EmployeeDocumentStatus.Okay;
        }

    }
}
