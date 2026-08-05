using System.Text.Json.Nodes;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface ISignDocumentService
    {
        Task<Guid> GenerateDocumentToSign(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default);

        Task<Guid> InsertDocumentToSign(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, Extension extension, Stream stream, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default);

        Task<string> ReceiveWebhookDocument(JsonNode contentBody, CancellationToken cancellationToken = default);

        Task InvalidateUnsignedDocument(Guid documentUnitId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default);

        // Disparo do envio agendado. [expectedSendOn] é a data que este disparo foi criado para atender: se o
        // agendamento gravado tiver outra, o agendamento foi trocado e este disparo é o antigo.
        Task SendScheduledDocumentToSign(Guid documentUnitId, Guid documentId, Guid companyId, DateOnly expectedSendOn,
            CancellationToken cancellationToken = default);
    }
}
