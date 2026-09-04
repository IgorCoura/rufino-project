namespace PeopleManagement.Application.Commands.DocumentCommands.CancelScheduledDocumentToSign
{
    /// <summary>
    /// Cancela o envio agendado da unidade. Sem agendamento, não faz nada — cancelar o que já não existe é a
    /// mesma intenção realizada.
    /// </summary>
    public record CancelScheduledDocumentToSignCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId)
        : IRequest<CancelScheduledDocumentToSignResponse>
    {
    }

    public record CancelScheduledDocumentToSignModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)
    {
        public CancelScheduledDocumentToSignCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company);
    }
}
