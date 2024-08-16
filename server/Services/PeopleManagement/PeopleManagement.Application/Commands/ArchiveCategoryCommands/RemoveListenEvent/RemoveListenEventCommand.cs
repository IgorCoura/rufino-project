namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent
{
    public record RemoveListenEventCommand(Guid ArchiveCategoryId, Guid CompanyId, int[] EventId) : IRequest<RemoveListenEventResponse>
    {
    }
}
