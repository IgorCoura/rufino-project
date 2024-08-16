using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using ArchiveFile = PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.File;

namespace PeopleManagement.Application.Commands.ArchiveCommands.InsertFile
{
    public sealed class InsertFileCommandHandler(IArchiveService archiveService, IArchiveRepository archiveRepository) : IRequestHandler<InsertFileCommand, InsertFileResponse>
    {
        public readonly IArchiveService _archiveService = archiveService;
        public readonly IArchiveRepository _archiveRepository = archiveRepository;
        public async Task<InsertFileResponse> Handle(InsertFileCommand request, CancellationToken cancellationToken)
        {
            var archiveId = await _archiveService.InsertFile(request.OwnerId, request.CompanyId, request.CategoryId, ArchiveFile.Create(Guid.NewGuid().ToString(), request.FileExtesion),
                request.stream, cancellation: cancellationToken);
            await _archiveRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return archiveId; 
        }
    }

    public class InsertFileIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<InsertFileCommand, InsertFileResponse>> logger) : IdentifiedCommandHandler<InsertFileCommand, InsertFileResponse>(mediator, logger)
    {
        protected override InsertFileResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
