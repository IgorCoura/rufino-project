
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;
using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent
{
    public sealed class RemoveListenEventCommandHandlers(IArchiveCategoryRepository archiveCategoryRepository) : IRequestHandler<RemoveListenEventCommand, RemoveListenEventResponse>
    {
        private readonly IArchiveCategoryRepository _archiveCategoryRepository = archiveCategoryRepository;
        public async Task<RemoveListenEventResponse> Handle(RemoveListenEventCommand request, CancellationToken cancellationToken)
        {
           var category = await _archiveCategoryRepository.FirstOrDefaultAsync(x => x.Id == request.ArchiveCategoryId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
            ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(ArchiveCategory), request.ArchiveCategoryId.ToString()));

            category.RemoveRangeListenEvent(request.EventId);

            await _archiveCategoryRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
    }

    public sealed class RemoveListenEventIdentifiedCommandHandlers(IMediator mediator, ILogger<IdentifiedCommandHandler<RemoveListenEventCommand, RemoveListenEventResponse>> logger) : IdentifiedCommandHandler<RemoveListenEventCommand, RemoveListenEventResponse>(mediator, logger)
    {
        protected override RemoveListenEventResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}

