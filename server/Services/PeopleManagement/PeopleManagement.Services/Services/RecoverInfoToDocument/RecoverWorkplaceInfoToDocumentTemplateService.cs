using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverWorkplaceInfoToDocumentTemplateService(IWorkplaceRepository workplaceRepository) : IRecoverWorkplaceInfoToDocumentTemplateService
    {
        private readonly IWorkplaceRepository _workplaceRepository = workplaceRepository;
        public async Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, CancellationToken cancellation = default)
        {
            var workplace = await _workplaceRepository.GetWorkplaceFromEmployeeId(employeeId, companyId, cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Workplace), employeeId.ToString()!));

            var workplaceJson = new JsonObject
            {
                ["Workplace"] = new JsonObject
                {
                    ["Id"] = workplace.Id.ToString(),
                    ["Name"] = workplace.Name.ToString(),
                    ["Address"] = ConvertAddressToJsonObject(workplace.Address)
                }
            };

            return workplaceJson;
        }

        public static JsonObject GetModel()
        {
            var address = Address.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil");

            var json = new JsonObject
            {
                ["Workplace"] = new JsonObject
                {
                    ["Id"] = "workplace.Id",
                    ["Name"] = "workplace.Name",
                    ["Address"] = ConvertAddressToJsonObject(Address.Create("69015-756", "Rua 11", "936", "", "Colônia Terra Nova", "Manaus", "Amazonia", "Brasil"))
                }
            };

            return json;
        }
        private static JsonObject ConvertAddressToJsonObject(Address address)
        {
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
    }
}
