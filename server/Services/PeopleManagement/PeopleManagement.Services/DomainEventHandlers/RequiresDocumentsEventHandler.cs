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
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using System.Reflection;
using System.Threading;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class RequiresDocumentsEventHandler(IRequireDocumentsRepository requireDocumentsRepository, IDocumentTemplateRepository documentTemplateRepository, IDocumentRepository documentRepository, IEmployeeRepository employeeRepository) : INotificationHandler<RequestDocumentsEvent>
    {
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task Handle(RequestDocumentsEvent notification, CancellationToken cancellationToken)
        {

            await RemoveUnnecessaryDocuments(notification, cancellationToken);

            RequireDocuments? requiresDocuments = await _requireDocumentsRepository.FirstOrDefaultAsync(x => x.AssociationId == notification.AssociationId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken);

            if (requiresDocuments is null)
                return;

            foreach (var templateId in requiresDocuments.DocumentsTemplatesIds)
            {
                Document? document = await _documentRepository.FirstOrDefaultAsync(x => x.DocumentTemplateId == templateId, cancellation: cancellationToken);

                if (document is not null)
                    continue;

                DocumentTemplate documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == templateId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken)
                    ?? throw new ArgumentNullException(nameof(DocumentTemplate));

                var documentId = Guid.NewGuid();
                document = Document.Create(documentId, notification.EmployeeId, notification.CompanyId, requiresDocuments.Id, templateId, documentTemplate.Name.Value, documentTemplate.Description.Value);
                await _documentRepository.InsertAsync(document, cancellationToken);
            }
        }

        public async Task RemoveUnnecessaryDocuments(RequestDocumentsEvent notification, CancellationToken cancellationToken)
        {
            var allEmployeeDocument = await _documentRepository.GetDataAsync(x => x.EmployeeId == notification.EmployeeId && x.CompanyId == notification.CompanyId && x.DocumentsUnits.Any() == false, cancellation: cancellationToken);

            foreach (var document in allEmployeeDocument)
            {
                RequireDocuments? reqDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(x => x.AssociationId == notification.AssociationId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken);

                if (reqDocument is null)
                {
                    await _documentRepository.DeleteAsync(document);
                    return;
                }

                Employee? employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == notification.EmployeeId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken);

                if (employee is null) 
                {
                    await _documentRepository.DeleteAsync(document);
                    return;
                }

                var properties = typeof(Employee).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.CanRead)
                   .ToList();

                bool hasMatchingProperty = properties.Any(property =>
                {
                    var value = property.GetValue(employee);
                    return value is Guid guidValue && guidValue == reqDocument!.AssociationId;
                });

                if (hasMatchingProperty == false)
                {
                    await _documentRepository.DeleteAsync(document);
                }
            }
        }
    }
}
