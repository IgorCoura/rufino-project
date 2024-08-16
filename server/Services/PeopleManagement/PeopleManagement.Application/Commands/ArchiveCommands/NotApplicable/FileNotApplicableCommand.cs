namespace PeopleManagement.Application.Commands.ArchiveCommands.NotApplicable
{
    public record FileNotApplicableCommand(Guid ArchiveId, Guid OwnerId, Guid CompanyId, string FileName) : IRequest<FileNotApplicableResponse>
    {
    }
}
