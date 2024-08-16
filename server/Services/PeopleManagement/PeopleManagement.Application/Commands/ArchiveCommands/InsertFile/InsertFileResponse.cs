using PeopleManagement.Application.Commands.DocumentCommands.InsertDocument;

namespace PeopleManagement.Application.Commands.ArchiveCommands.InsertFile
{
    public record InsertFileResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator InsertFileResponse(Guid id) => new(id);
    }
}
