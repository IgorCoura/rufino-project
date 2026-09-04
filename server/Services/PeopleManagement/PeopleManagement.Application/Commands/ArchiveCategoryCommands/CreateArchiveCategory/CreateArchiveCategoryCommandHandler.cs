#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public sealed class CreateArchiveCategoryCommandHandler(IArchiveCategoryService archiveCategoryService, IArchiveCategoryRepository archiveCategoryRepository) : IRequestHandler<CreateArchiveCategoryCommand, CreateArchiveCategoryResponse>
    {
        private readonly IArchiveCategoryRepository _archiveCategoryRepository = archiveCategoryRepository;
        private readonly IArchiveCategoryService _archiveCategoryService = archiveCategoryService;
        public async Task<CreateArchiveCategoryResponse> Handle(CreateArchiveCategoryCommand request, CancellationToken cancellationToken)
        {
            var archiveCategorieId = await _archiveCategoryService.CrateArchiveCategory(request.Name, request.Description, request.CompanyId, request.ListenEventsIds, cancellationToken);
            await _archiveCategoryRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return archiveCategorieId;
        }
    }

    public sealed class InsertFileIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<CreateArchiveCategoryCommand, CreateArchiveCategoryResponse>> logger, IRequestManager requestManager) : IdentifiedCommandHandler<CreateArchiveCategoryCommand, CreateArchiveCategoryResponse>(mediator, logger, requestManager)
    {
        protected override CreateArchiveCategoryResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
