using MediatR;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class RequiresDocumentsEventHandler(IRequireDocumentsRepository requireSecurityDocumentsRepository, IDocumentTemplateRepository documentTemplateRepository, IDocumentRepository securityDocumentRepository) : INotificationHandler<RequestDocumentsEvent>
    {
        private readonly IRequireDocumentsRepository _requireSecurityDocumentsRepository = requireSecurityDocumentsRepository;
        private readonly IDocumentRepository _securityDocumentRepository = securityDocumentRepository;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;

        public async Task Handle(RequestDocumentsEvent notification, CancellationToken cancellationToken)
        {
            var requiresDocuments = await _requireSecurityDocumentsRepository.FirstOrDefaultAsync(x => x.RoleId == notification.RoleId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(RequireDocuments), notification.RoleId.ToString()));

            foreach(var templateId in requiresDocuments.DocumentsTemplatesIds)
            {
                var documentId = Guid.NewGuid();
                var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == templateId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken)
                    ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), templateId.ToString()));
                var document = Document.Create(documentId, notification.EmployeeId, notification.CompanyId, notification.RoleId, templateId, documentTemplate.Name.ToString(), documentTemplate.Description.ToString());
                await _securityDocumentRepository.InsertAsync(document, cancellationToken);
            }
        }
    }
}
