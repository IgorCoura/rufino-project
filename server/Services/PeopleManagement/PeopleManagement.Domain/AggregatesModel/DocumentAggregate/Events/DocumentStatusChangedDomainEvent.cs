using MediatR;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events
{
    public class DocumentStatusChangedDomainEvent : INotification
    {
        public Guid DocumentId { get; }
        public Guid EmployeeId { get; }
        public Guid CompanyId { get; }
        public DocumentStatus OldStatus { get; }
        public DocumentStatus NewStatus { get; }

        public DocumentStatusChangedDomainEvent(Guid documentId, Guid employeeId, Guid companyId, DocumentStatus oldStatus, DocumentStatus newStatus)
        {
            DocumentId = documentId;
            EmployeeId = employeeId;
            CompanyId = companyId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}
