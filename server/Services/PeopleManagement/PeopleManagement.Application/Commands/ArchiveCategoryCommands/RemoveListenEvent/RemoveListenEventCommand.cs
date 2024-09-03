namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent
{
    public record RemoveListenEventCommand(Guid ArchiveCategoryId, Guid CompanyId, int[] EventId) : IRequest<RemoveListenEventResponse>
    {
    }

    public record RemoveListenEventModel(Guid ArchiveCategoryId, int[] EventId)
    {
        public RemoveListenEventCommand ToCommand(Guid company) => new(ArchiveCategoryId, company, EventId);
    }
}
