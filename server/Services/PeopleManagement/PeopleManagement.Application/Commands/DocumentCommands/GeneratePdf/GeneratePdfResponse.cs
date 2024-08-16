using PeopleManagement.Application.Commands.DocumentCommands.CreateDocument;

namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf
{
    public record GeneratePdfResponse(Guid Id, byte[] Pdf) : BaseDTO(Id)
    {
        public static implicit operator GeneratePdfResponse(Guid id) => new(id);
    }
}
