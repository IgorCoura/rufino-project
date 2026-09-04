#pragma warning disable CS0618 // Ultimo consumidor VIVO da feature Archive: o fluxo de
// admissao ainda cria e confere os arquivos por evento. A superficie HTTP foi removida em
// 2026-09-04, esta nao. Enquanto isto existir, Archive nao pode ser apagado do banco.
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
