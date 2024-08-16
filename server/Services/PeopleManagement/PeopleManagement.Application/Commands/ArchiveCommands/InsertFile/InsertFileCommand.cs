namespace PeopleManagement.Application.Commands.ArchiveCommands.InsertFile
{
    public record InsertFileCommand(Guid OwnerId, Guid CompanyId, Guid CategoryId, string FileExtesion, Stream stream) : IRequest<InsertFileResponse>
    {
    }
}
