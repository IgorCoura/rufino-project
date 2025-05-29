using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using System.Text.Json.Nodes;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using System.Text.Json;

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
                ["Id"] = workplace.Id.ToString(),
                ["Name"] = workplace.Name.ToString(),
                ["Address"] = JsonSerializer.Serialize(workplace.Address)
            };

            return workplaceJson;
        }
    }
}
