namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.EditDescriptionArchiveCategory
{
    public record EditDescriptionArchiveCategoryResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator EditDescriptionArchiveCategoryResponse(Guid id) => new(id);
    }
}
