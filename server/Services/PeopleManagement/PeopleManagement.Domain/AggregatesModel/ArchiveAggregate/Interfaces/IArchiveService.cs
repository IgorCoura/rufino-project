namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces
{
    public interface IArchiveService
    {
        Task CreateFilesForEvent(Guid ownerId, Guid companyId, int eventId, CancellationToken cancellationToken = default);
        Task<Guid> InsertFile(Guid ownerId, Guid companyId, Guid categoryId, File file, Stream stream, CancellationToken cancellation = default);
        Task<bool> HasRequiresFiles(Guid ownerId, Guid companyId);
    }
}

