﻿using MediatR;
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
using Microsoft.Extensions.Logging;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class RequiresDocumentsEventHandler(
        IRequireDocumentsRepository requireDocumentsRepository, 
        IDocumentTemplateRepository documentTemplateRepository, 
        IDocumentRepository documentRepository, 
        IEmployeeRepository employeeRepository,
        ILogger<RequiresDocumentsEventHandler> logger) 
        : INotificationHandler<RequestDocumentsEvent>
    {
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly ILogger<RequiresDocumentsEventHandler> _logger = logger;

        public async Task Handle(RequestDocumentsEvent notification, CancellationToken cancellationToken)
        {

            await RemoveUnnecessaryDocuments(notification, cancellationToken);

            var requiresDocuments = (await _requireDocumentsRepository.GetDataAsync(x => 
                x.AssociationId == notification.AssociationId && x.CompanyId == notification.CompanyId,
                cancellation: cancellationToken)).ToList();

            if (requiresDocuments.Count <= 0)
            {
                _logger.LogWarning("No required documents found for association {AssociationId} in company {CompanyId}.",
                    notification.AssociationId, notification.CompanyId);
                return;
            }

            foreach(var requireDocument in requiresDocuments)
            {
                foreach (var templateId in requireDocument.DocumentsTemplatesIds)
                {
                    Document? document = await _documentRepository.FirstOrDefaultAsync(x => x.DocumentTemplateId == templateId, cancellation: cancellationToken);

                    if (document is not null)
                        continue;

                    DocumentTemplate? documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x =>
                    x.Id == templateId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken);

                    if (documentTemplate is null)
                    {
                        _logger.LogError("Document template with ID {TemplateId} not found for company {CompanyId}.",
                            templateId, notification.CompanyId);
                        continue;
                    }

                    var documentId = Guid.NewGuid();
                    document = Document.Create(documentId, notification.EmployeeId, notification.CompanyId, requireDocument.Id, templateId, documentTemplate.Name.Value, documentTemplate.Description.Value);
                    await _documentRepository.InsertAsync(document, cancellationToken);
                }
            }
        }

        public async Task RemoveUnnecessaryDocuments(RequestDocumentsEvent notification, CancellationToken cancellationToken)
        {
            
            var allEmployeeDocument = await _documentRepository.GetDataAsync(x => x.EmployeeId == notification.EmployeeId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken);

            foreach (var document in allEmployeeDocument)
            {               

                RequireDocuments? reqDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(x => x.Id == document.RequiredDocumentId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken);

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


                bool isAssociation = employee.IsAssociation(reqDocument.AssociationId);

                if (isAssociation == false)
                {
                    document.MakeAsDeprecated();

                    if (document.CanBeDeleted())
                    {
                        await _documentRepository.DeleteAsync(document);
                        
                    }
                    return;
                }
            }
        }
    }
}
