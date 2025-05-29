using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverCompanyInfoToDocumentTemplateService(ICompanyRepository companyRepository) : IRecoverCompanyInfoToDocumentTemplateService
    {
        private readonly ICompanyRepository _companyRepository = companyRepository;
        public async Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, CancellationToken cancellation = default)
        {
            var company = await _companyRepository.FirstOrDefaultAsync(x => x.Id == companyId, cancellation: cancellation)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Company), companyId.ToString()));

            var companyJson = new JsonObject
            {
                ["Id"] = company.Id.ToString(),
                ["CorporateName"] = company.CorporateName.ToString(),
                ["FantasyName"] = company.FantasyName.ToString(),
                ["Cnpj"] = company.Cnpj.ToString(),
                ["Contact"] = JsonSerializer.Serialize(company.Contact),
                ["Address"] = JsonSerializer.Serialize(company.Address)
            };

            return companyJson;
        }
    }
}
