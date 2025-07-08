using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using System.Text.Json.Nodes;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverPositionInfoToDocumentTemplateService(IPositionRepository positionRepository) : IRecoverPositionInfoToDocumentTemplateService
    {
        private readonly IPositionRepository _positionRepository = positionRepository;  
        public async Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, JsonObject[]? jsonObjects = null, CancellationToken cancellation = default)
        {
            var position = await _positionRepository.GetPositionFromEmployeeId(employeeId, companyId, cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Position), employeeId.ToString()!));

            var positionJson = new JsonObject
            {
                ["Position"] = new JsonObject
                {
                    ["Id"] = position.Id.ToString(),
                    ["Name"] = position.Name.ToString(),
                    ["Description"] = position.Description.ToString(),
                    ["CBO"] = position.CBO.ToString()
                }
            };

            return positionJson;
        }

        public static JsonObject GetModel()
        {
           
            var json = new JsonObject
            {
                ["Position"] = new JsonObject
                {
                    ["Id"] = Guid.Empty.ToString(),
                    ["Name"] = "position.Name",
                    ["Description"] = "position.Description",
                    ["CBO"] = "position.CBO"
                }
            };

            return json;
        }

    }
}
