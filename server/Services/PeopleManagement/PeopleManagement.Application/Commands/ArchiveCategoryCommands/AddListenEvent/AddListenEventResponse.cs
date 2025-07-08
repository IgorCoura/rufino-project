namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent
{
    public record AddListenEventResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AddListenEventResponse(Guid id) => new(id);
    }
}
