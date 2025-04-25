using MediatR;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using System.Diagnostics;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class EmployeeEventHandler(IArchiveService archiveService, IDocumentService documentService) : INotificationHandler<EmployeeEvent>
    {
        private readonly IArchiveService _archiveService = archiveService;
        private readonly IDocumentService _documentService = documentService;

        public async Task Handle(EmployeeEvent notification, CancellationToken cancellationToken)
        {
            await _archiveService.CreateFilesForEvent(notification.EmployeeId, notification.CompanyId, notification.Id, cancellationToken);
            await _documentService.CreateDocumentUnitsForEvent(notification.EmployeeId, notification.CompanyId, notification.Id, cancellationToken);
        }
    }
}
