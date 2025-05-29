using System.Text.Json.Nodes;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces
{
    public interface IRecoverInfoToDocumentTemplateService
    {
        Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, CancellationToken cancellation = default);
    }
}
