using PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent;

namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.EditDescriptionArchiveCategory
{
    public record EditDescriptionArchiveCategoryCommand(Guid ArchiveCategoryId, Guid CompanyId, String Description) : IRequest<EditDescriptionArchiveCategoryResponse>
    {
    }

    public record EditDescriptionArchiveCategoryModel(Guid ArchiveCategoryId, String Description)
    {
        public EditDescriptionArchiveCategoryCommand ToCommand(Guid company) => new(ArchiveCategoryId, company ,Description);
    }
}
