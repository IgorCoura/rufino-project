using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Contact = PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Contact;
using Address = PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Address;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverCompanyInfoToDocumentTemplateService(ICompanyRepository companyRepository) : IRecoverCompanyInfoToDocumentTemplateService
    {
        private readonly ICompanyRepository _companyRepository = companyRepository;
        public async Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, JsonObject[]? jsonObjects = null, CancellationToken cancellation = default)
        {
            var company = await _companyRepository.FirstOrDefaultAsync(x => x.Id == companyId, cancellation: cancellation)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Company), companyId.ToString()));

            var companyJson = new JsonObject
            {
                ["Company"] = new JsonObject {
                    ["Id"] = company.Id.ToString(),
                    ["CorporateName"] = company.CorporateName.ToString(),
                    ["FantasyName"] = company.FantasyName.ToString(),
                    ["Cnpj"] = company.Cnpj.ToString(),
                    ["Contact"] = ConvertContactToJsonObject(company.Contact),
                    ["Address"] = ConvertAddressToJsonObject(company.Address) 
                }
            };

            return companyJson;
        }

        public static JsonObject GetModel()
        {
            var json = new JsonObject
            {
                ["Company"] = new JsonObject
                {
                    ["Id"] = Guid.Empty.ToString(),
                    ["CorporateName"] = "company.CorporateName",
                    ["FantasyName"] = "company.FantasyName",
                    ["Cnpj"] = "company.Cnpj",
                    ["Contact"] = ConvertContactToJsonObject(Contact.Create("email@email.com", "11911111111")),
                    ["Address"] = ConvertAddressToJsonObject(Address.Create(
                                    "14093636",
                                    "Rua José Otávio de Oliveira",
                                    "776",
                                    "",
                                    "Parque dos Flamboyans",
                                    "Ribeirão Preto",
                                    "SP",
                                    "BRASIL"))
                }
            };

            return json;
        }

        private static JsonObject ConvertAddressToJsonObject(Address? address)
        {
            if (address == null)
            {
                return new JsonObject
                {
                };
            }

            return new JsonObject
            {
                ["ZipCode"] = address.ZipCode,
                ["Street"] = address.Street,
                ["Number"] = address.Number,
                ["Complement"] = address.Complement,
                ["Neighborhood"] = address.Neighborhood,
                ["City"] = address.City,
                ["State"] = address.State,
                ["Country"] = address.Country
            };
        }
        private static JsonObject ConvertContactToJsonObject(Contact? contact)
        {
            if (contact == null)
            {
                return new JsonObject
                {
                };
            }

            return new JsonObject
            {
                ["Email"] = contact.Email,
                ["Phone"] = contact.Phone
            };
        }

      
    }
}
