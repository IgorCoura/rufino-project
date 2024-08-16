namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent
{
    public record AddListenEventCommand(Guid ArchiveCategoryId, Guid CompanyId, int[] EventId) : IRequest<AddListenEventResponse>
    {
    }
}
