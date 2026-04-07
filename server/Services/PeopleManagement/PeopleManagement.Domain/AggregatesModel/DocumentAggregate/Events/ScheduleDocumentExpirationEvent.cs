namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events
{
    public record ScheduleDocumentExpirationEvent : INotification
    {
        
        public Guid DocumentId { get; private set; }
        public Guid DocumentUnitId { get; private set; }
        public Guid CompanyId { get; set; }
        public DateOnly Expiration { get; set; }
        public DateOnly IssuedAt { get; set; }

        private ScheduleDocumentExpirationEvent(Guid documentId, Guid documentUnitId, Guid companyId, DateOnly expiration, DateOnly issuedAt)
        {
            DocumentId = documentId;
            DocumentUnitId = documentUnitId;
            CompanyId = companyId;
            Expiration = expiration;
            IssuedAt = issuedAt;
        }


        public static ScheduleDocumentExpirationEvent Create(Guid documentId, Guid documentUnitId, Guid companyId, DateOnly expiration, DateOnly issuedAt) => new(documentId, documentUnitId, companyId, expiration, issuedAt);

    }
}
