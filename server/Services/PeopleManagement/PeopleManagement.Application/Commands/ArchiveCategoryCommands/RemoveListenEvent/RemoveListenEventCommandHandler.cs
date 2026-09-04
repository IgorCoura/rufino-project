#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.

using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
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

    public sealed class RemoveListenEventIdentifiedCommandHandlers(IMediator mediator, ILogger<IdentifiedCommandHandler<RemoveListenEventCommand, RemoveListenEventResponse>> logger, IRequestManager requestManager) : IdentifiedCommandHandler<RemoveListenEventCommand, RemoveListenEventResponse>(mediator, logger, requestManager)
    {
        protected override RemoveListenEventResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}

