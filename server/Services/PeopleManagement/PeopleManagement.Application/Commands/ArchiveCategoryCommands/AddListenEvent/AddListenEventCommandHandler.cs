#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public sealed class AddListenEventCommandHandler(IArchiveCategoryRepository archiveCategoryRepository, IArchiveCategoryService archiveCategoryService) : IRequestHandler<AddListenEventCommand, AddListenEventResponse>
    {
        private readonly IArchiveCategoryRepository _archiveCategoryRepository = archiveCategoryRepository;
        private readonly IArchiveCategoryService _archiveCategoryService = archiveCategoryService;
        public async Task<AddListenEventResponse> Handle(AddListenEventCommand request, CancellationToken cancellationToken)
        {
            await _archiveCategoryService.AddListenEvent(request.ArchiveCategoryId, request.CompanyId, request.EventId, cancellationToken: cancellationToken);
            await _archiveCategoryRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return request.ArchiveCategoryId;
        }
    }

    public sealed class AddListenEventIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AddListenEventCommand, AddListenEventResponse>> logger, IRequestManager requestManager) : IdentifiedCommandHandler<AddListenEventCommand, AddListenEventResponse>(mediator, logger, requestManager)
    {
        protected override AddListenEventResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
