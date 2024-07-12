using MediatR;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class RequiresSecurityDocumentsEventHandler(IRequireSecurityDocumentsRepository requireSecurityDocumentsRepository, ISecurityDocumentRepository securityDocumentRepository) : INotificationHandler<RequestSecurityDocumentsEvent>
    {
        private readonly IRequireSecurityDocumentsRepository _requireSecurityDocumentsRepository = requireSecurityDocumentsRepository;
        private readonly ISecurityDocumentRepository _securityDocumentRepository = securityDocumentRepository;

        public async Task Handle(RequestSecurityDocumentsEvent notification, CancellationToken cancellationToken)
        {
            var requiresDocuments = await _requireSecurityDocumentsRepository.FirstOrDefaultAsync(x => x.RoleId == notification.RoleId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(RequireSecurityDocuments), notification.RoleId.ToString()));

            foreach(var templates in requiresDocuments.DocumentsTemplatesIds)
            {
                var securityDocumentId = Guid.NewGuid();
                var securityDocument = SecurityDocument.Create(securityDocumentId, notification.EmployeeId, notification.CompanyId, notification.RoleId, templates);
                await _securityDocumentRepository.InsertAsync(securityDocument, cancellationToken);
            }
        }
    }
}
