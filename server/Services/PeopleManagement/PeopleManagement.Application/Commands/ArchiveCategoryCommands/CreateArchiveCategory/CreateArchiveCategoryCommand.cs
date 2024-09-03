using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory
{
    public record CreateArchiveCategoryCommand(string Name, string Description, int[] ListenEventsIds, Guid CompanyId) : IRequest<CreateArchiveCategoryResponse>
    {
        public ArchiveCategory ToArchiveCategory(Guid id) => ArchiveCategory.Create(id, Name, Description, [.. ListenEventsIds], CompanyId);
    }

    public record CreateArchiveCategoryModel(string Name, string Description, int[] ListenEventsIds)
    {
        public CreateArchiveCategoryCommand ToCommand(Guid company) => new(Name, Description, ListenEventsIds, company);
    }
}
