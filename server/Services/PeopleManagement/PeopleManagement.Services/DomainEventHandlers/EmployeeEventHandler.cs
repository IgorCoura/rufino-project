using MediatR;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using System.Diagnostics;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class EmployeeEventHandler(IArchiveService archiveService, IDocumentService documentService,
        IDocumentDepreciationService documentDepreciationService) : INotificationHandler<EmployeeEvent>
    {
        private readonly IArchiveService _archiveService = archiveService;
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentDepreciationService _documentDepreciationService = documentDepreciationService;

        public async Task Handle(EmployeeEvent notification, CancellationToken cancellationToken)
        {
            // Antes de gerar as unidades do novo contrato: a conclusão da admissão é o único caminho que abre um
            // contrato de trabalho (Employee.NewContract é privado e só CompleteAdmission o chama), então é aqui
            // que os documentos entregues no contrato anterior deixam de valer. Depreciar primeiro deixa o
            // documento no estado final antes de a geração acrescentar as pendentes do vínculo novo.
            if (notification.Id == EmployeeEvent.COMPLETE_ADMISSION_EVENT)
                await _documentDepreciationService.DeprecateDocumentsForNewContract(notification.EmployeeId, notification.CompanyId, cancellationToken);

            await _archiveService.CreateFilesForEvent(notification.EmployeeId, notification.CompanyId, notification.Id, cancellationToken);
            await _documentService.CreateDocumentUnitsForEvent(notification.EmployeeId, notification.CompanyId, notification.Id, cancellationToken);
        }
    }
}
