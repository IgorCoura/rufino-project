namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent
{
    public record RemoveListenEventResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator RemoveListenEventResponse(Guid id) => new(id);
    }
}
