using MediatR;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.Events;

namespace PeopleManagement.Services.DomainEventHandlers
{
    public class RequiresDocumentsEventHandler : INotificationHandler<RequiresDocumentsEvent>
    {
        private readonly IArchiveService _archiveService;
        private readonly IArchiveRepository _archiveRepository;
        public RequiresDocumentsEventHandler(IArchiveService archiveService, IArchiveRepository archiveRepository)
        {
            _archiveService = archiveService;
            _archiveRepository = archiveRepository;
        }

        public async Task Handle(RequiresDocumentsEvent notification, CancellationToken cancellationToken)
        {
            await _archiveService.RequiresFiles(notification.OwnerId, notification.CompanyId, notification.Categories);
            await _archiveRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
