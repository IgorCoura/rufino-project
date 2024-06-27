using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;

namespace PeopleManagement.Services.Services.RecoverInfoToSecurityDocument
{
    public class RecoverNR18InfoToSegurityDocumentService : IRecoverInfoToSegurityDocumentService
    {
        private readonly ICompanyRepository _companyRepository;

        public RecoverNR18InfoToSegurityDocumentService(ICompanyRepository companyRepository)
        {
            _companyRepository = companyRepository;
        }

        public Task<Dictionary<string, dynamic>> RecoverInfo()
        {
            throw new NotImplementedException();
        }
    }
}
