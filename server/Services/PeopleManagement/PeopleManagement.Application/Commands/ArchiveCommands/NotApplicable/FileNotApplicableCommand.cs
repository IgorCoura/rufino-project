namespace PeopleManagement.Application.Commands.ArchiveCommands.NotApplicable
{
    public record FileNotApplicableCommand(Guid ArchiveId, Guid OwnerId, Guid CompanyId, string FileName) : IRequest<FileNotApplicableResponse>
    {
    }

    public record FileNotApplicableModel(Guid ArchiveId, Guid OwnerId, string FileName)
    {
        public FileNotApplicableCommand ToCommand(Guid company) => new(ArchiveId, OwnerId, company, FileName);
    }
}
