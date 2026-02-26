using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.Services
{
    public class EmployeeDocumentStatusService : IEmployeeDocumentStatusService
    {
        public EmployeeDocumentStatus DetermineStatusFromDocumentStatuses(List<DocumentStatus> documentStatuses)
        {

            //return EmployeeDocumentStatus.Okay;

            if (documentStatuses == null || documentStatuses.Count == 0)
            {
                return EmployeeDocumentStatus.Okay;
            }

            // Prioridade 1: Warning
            if (documentStatuses.Any(x => x == DocumentStatus.Warning))
            {
                return EmployeeDocumentStatus.Warning;
            }

            // Prioridade 2: Qualquer status que não seja OK ou AwaitingSignature
            if (documentStatuses.Any(x => x != DocumentStatus.OK && x != DocumentStatus.AwaitingSignature))
            {
                return EmployeeDocumentStatus.RequiresAttention;
            }

            return EmployeeDocumentStatus.Okay;
        }
    }
}
