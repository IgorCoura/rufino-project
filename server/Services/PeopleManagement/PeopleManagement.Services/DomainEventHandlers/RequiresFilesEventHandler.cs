using MediatR;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class RequiresFilesEventHandler(IArchiveService archiveService, IArchiveRepository archiveRepository) : INotificationHandler<RequestFilesEvent>
    {
        private readonly IArchiveService _archiveService = archiveService;
        private readonly IArchiveRepository _archiveRepository = archiveRepository;

        public async Task Handle(RequestFilesEvent notification, CancellationToken cancellationToken)
        {
            await _archiveService.RequiresFiles(notification.OwnerId, notification.CompanyId, notification.Id);
            await _archiveRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
