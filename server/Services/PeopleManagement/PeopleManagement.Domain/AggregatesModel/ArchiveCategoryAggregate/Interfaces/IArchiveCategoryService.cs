namespace PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces
{
    public interface IArchiveCategoryService
    {
        Task<Guid> CrateArchiveCategory(string Name, string Description, Guid CompanyId, int[] ListenEventsIds, CancellationToken cancellationToken);
        Task AddListenEvent(Guid archiveCategoryId, Guid companyId, int[] eventIds, CancellationToken cancellationToken);
    }
}
