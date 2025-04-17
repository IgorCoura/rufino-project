using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.EditDescriptionArchiveCategory
{
    public sealed class EditDescriptionArchiveCategoryCommandHandler(IArchiveCategoryRepository archiveCategoryRepository, IArchiveCategoryService archiveCategoryService) : IRequestHandler<EditDescriptionArchiveCategoryCommand, EditDescriptionArchiveCategoryResponse>
    {
        private readonly IArchiveCategoryRepository _archiveCategoryRepository = archiveCategoryRepository;
        private readonly IArchiveCategoryService _archiveCategoryService = archiveCategoryService;
        public async Task<EditDescriptionArchiveCategoryResponse> Handle(EditDescriptionArchiveCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _archiveCategoryRepository.FirstOrDefaultAsync(x => x.Id == request.ArchiveCategoryId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
           ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(ArchiveCategory), request.ArchiveCategoryId.ToString()));

            category.Description = request.Description;

            await _archiveCategoryRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
    }

    public sealed class EditDescriptionArchiveCategoryIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<EditDescriptionArchiveCategoryCommand, EditDescriptionArchiveCategoryResponse>> logger) : IdentifiedCommandHandler<EditDescriptionArchiveCategoryCommand, EditDescriptionArchiveCategoryResponse>(mediator, logger)
    {
        protected override EditDescriptionArchiveCategoryResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
