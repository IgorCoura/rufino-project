using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;

namespace PeopleManagement.Services.Services.RecoverInfoToSecurityDocument
{
    public class RecoverNR01InfoToSegurityDocumentService : IRecoverNR01InfoToSegurityDocumentService
    {
        private readonly IEmployeeRepository _employeeRepository;

        public RecoverNR01InfoToSegurityDocumentService(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public Task<string> RecoverInfo()
        {
            throw new NotImplementedException();
        }
    }
}
