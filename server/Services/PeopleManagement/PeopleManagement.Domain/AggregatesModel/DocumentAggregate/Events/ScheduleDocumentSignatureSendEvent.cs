namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events
{
    /// <summary>
    /// Um envio para assinatura foi agendado para <see cref="SendOn"/>. Quem escuta agenda o disparo — o
    /// domínio não conhece o agendador, do mesmo jeito que em <see cref="ScheduleDocumentExpirationEvent"/>.
    ///
    /// <see cref="SendOn"/> viaja no evento (e não só no banco) porque o disparo o compara com o agendamento
    /// gravado na hora de executar: se não bater, o agendamento foi trocado e este disparo é o antigo.
    /// </summary>
    public record ScheduleDocumentSignatureSendEvent : INotification
    {
        public Guid DocumentId { get; private set; }
        public Guid DocumentUnitId { get; private set; }
        public Guid CompanyId { get; private set; }
        public DateOnly SendOn { get; private set; }

        private ScheduleDocumentSignatureSendEvent(Guid documentId, Guid documentUnitId, Guid companyId, DateOnly sendOn)
        {
            DocumentId = documentId;
            DocumentUnitId = documentUnitId;
            CompanyId = companyId;
            SendOn = sendOn;
        }

        public static ScheduleDocumentSignatureSendEvent Create(Guid documentId, Guid documentUnitId, Guid companyId, DateOnly sendOn)
            => new(documentId, documentUnitId, companyId, sendOn);
    }
}
