namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory
{
    public record CreateArchiveCategoryResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateArchiveCategoryResponse(Guid id) => new(id);
    }
}
