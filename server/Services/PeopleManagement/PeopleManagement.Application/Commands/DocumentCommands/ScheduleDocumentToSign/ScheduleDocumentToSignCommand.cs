namespace PeopleManagement.Application.Commands.DocumentCommands.ScheduleDocumentToSign
{
    /// <summary>
    /// Agenda o envio da unidade para assinatura em <see cref="SendOn"/>, em vez de enviá-la agora. Espelha o
    /// contrato do envio imediato (GenerateDocumentToSign) com a data do disparo a mais.
    /// </summary>
    public record ScheduleDocumentToSignCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId,
        DateOnly SendOn, DateOnly DateLimitToSign, int EminderEveryNDays) : IRequest<ScheduleDocumentToSignResponse>
    {
    }

    public record ScheduleDocumentToSignModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId,
        DateOnly SendOn, DateOnly DateLimitToSign, int EminderEveryNDays)
    {
        public ScheduleDocumentToSignCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company,
            SendOn, DateLimitToSign, EminderEveryNDays);
    }
}
