using MediatR;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class RequiresDocumentsEventHandler(IArchiveService archiveService, IArchiveRepository archiveRepository) : INotificationHandler<RequestDocumentsEvent>
    {
        private readonly IArchiveService _archiveService = archiveService;
        private readonly IArchiveRepository _archiveRepository = archiveRepository;

        public async Task Handle(RequestDocumentsEvent notification, CancellationToken cancellationToken)
        {
            await _archiveService.RequiresFiles(notification.OwnerId, notification.CompanyId, notification.Categories);
            await _archiveRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
