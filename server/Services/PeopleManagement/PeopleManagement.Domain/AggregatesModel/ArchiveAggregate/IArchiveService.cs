namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public interface IArchiveService
    {
        Task RequiresFile(Guid ownerId, Guid companyId, Category category);
        Task RequiresFiles(Guid ownerId, Guid companyId, Category[] category);
        Task InsertFile(Guid ownerId, Guid companyId, Category category, File file, Stream stream);

        Task<IEnumerable<Archive>> GetArchivesWithRequiresFiles(Guid ownerId, Guid companyId);
    }
}
