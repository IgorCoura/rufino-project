namespace PeopleManagement.Application.Commands.ArchiveCommands.InsertFile
{
    public record InsertFileCommand(Guid OwnerId, Guid CompanyId, Guid CategoryId, string FileExtesion, Stream stream) : IRequest<InsertFileResponse>
    {
    }

    public record InsertFileModel(Guid OwnerId, Guid CategoryId)
    {
        public InsertFileCommand ToCommand(Guid company, string FileExtesion, Stream stream) => new(OwnerId, company, CategoryId, FileExtesion, stream);
    }
}

