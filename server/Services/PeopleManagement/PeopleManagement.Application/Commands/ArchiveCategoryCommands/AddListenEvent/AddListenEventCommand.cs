namespace PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent
{
    public record AddListenEventCommand(Guid ArchiveCategoryId, int[] EventId, Guid CompanyId) : IRequest<AddListenEventResponse>
    {
    }

    public record AddListenEventModel(Guid ArchiveCategoryId, int[] EventId)
    {
        public AddListenEventCommand ToCommand(Guid company) => new(ArchiveCategoryId, EventId, company);
    }
}
