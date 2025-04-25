using MediatR;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using System.Diagnostics;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class EmployeeEventHandler(IArchiveService archiveService) : INotificationHandler<EmployeeEvent>
    {
        private readonly IArchiveService _archiveService = archiveService;

        public async Task Handle(EmployeeEvent notification, CancellationToken cancellationToken)
        {
            Debug.WriteLine($"EmployeeEvent-> EmployeeId: {notification.EmployeeId}, EventId: {notification.Id}, NameEvent: {notification.Name}, CompanyId: {notification.CompanyId}");
            await _archiveService.RequiresFiles(notification.EmployeeId, notification.CompanyId, notification.Id, cancellationToken);
        }
    }
}
