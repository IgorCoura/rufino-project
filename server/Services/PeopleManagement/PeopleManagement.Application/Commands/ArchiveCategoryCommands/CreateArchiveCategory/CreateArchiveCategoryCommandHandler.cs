using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory
{
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

    public sealed class InsertFileIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<CreateArchiveCategoryCommand, CreateArchiveCategoryResponse>> logger) : IdentifiedCommandHandler<CreateArchiveCategoryCommand, CreateArchiveCategoryResponse>(mediator, logger)
    {
        protected override CreateArchiveCategoryResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
