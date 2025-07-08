using System.Text.Json.Nodes;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface ISignDocumentService
    {
        Task<Guid> GenerateDocumentToSign(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default);

        Task<Guid> InsertDocumentToSign(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, Extension extension, Stream stream, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default);

        Task<Guid> InsertDocumentSigned(JsonNode contentBody, CancellationToken cancellationToken = default);
    }
}
