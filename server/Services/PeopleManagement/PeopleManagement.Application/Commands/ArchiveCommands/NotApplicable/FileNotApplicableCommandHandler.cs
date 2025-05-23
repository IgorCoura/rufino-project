using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.ArchiveCommands.NotApplicable
{
    public sealed class FileNotApplicableCommandHandler(IArchiveRepository archiveRepository) :
        IRequestHandler<FileNotApplicableCommand, FileNotApplicableResponse>
    {
        private readonly IArchiveRepository _archiveRepository = archiveRepository;
        public async Task<FileNotApplicableResponse> Handle(FileNotApplicableCommand request, CancellationToken cancellationToken)
        {
            var category = await _archiveRepository.FirstOrDefaultAsync(x => x.Id == request.ArchiveId && x.OwnerId == request.OwnerId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
            ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Archive), request.ArchiveId.ToString()));

            category.DocumentNotApplicable(request.FileName);

            await _archiveRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
    }

    public sealed class FileNotApplicableIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<FileNotApplicableCommand, FileNotApplicableResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<FileNotApplicableCommand, FileNotApplicableResponse>(mediator, logger, requestManager)
    {
        protected override FileNotApplicableResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
