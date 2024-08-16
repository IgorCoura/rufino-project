using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory
{
    public record CreateArchiveCategoryCommand(string Name, string Description, Guid CompanyId, int[] ListenEventsIds) : IRequest<CreateArchiveCategoryResponse>
    {
        public ArchiveCategory ToArchiveCategory(Guid id) => ArchiveCategory.Create(id, Name, Description, ListenEventsIds.ToList(), CompanyId);
    }
}
