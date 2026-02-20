using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;


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

            var allEmployeeDocument = await _documentRepository.GetDataAsync(x => x.EmployeeId == notification.EmployeeId && x.CompanyId == notification.CompanyId, include: i => i.Include(x => x.DocumentsUnits), cancellation: cancellationToken);

            foreach (var document in allEmployeeDocument)
            {
                RequireDocuments? reqDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(x => x.Id == document.RequiredDocumentId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken);
                Employee? employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == notification.EmployeeId && x.CompanyId == notification.CompanyId, cancellation: cancellationToken);

                if (employee is not null && reqDocument is not null)
                {
                    bool isAssociation = employee.IsAssociation(reqDocument.AssociationId);

                    if (isAssociation)
                    {
                        continue;
                    }
                }

                if (document.CanBeDeleted())
                {
                    await _documentRepository.DeleteAsync(document);

                }

            }
        }

    }
}
