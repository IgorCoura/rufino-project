using PeopleManagement.Application.Commands.SecurityDocumentCommands.CreateDocument;

namespace PeopleManagement.Application.Commands.SecurityDocumentCommands.GeneratePdf
{
    public record GeneratePdfResponse(Guid Id, byte[] Pdf) : BaseDTO(Id)
    {
        public static implicit operator GeneratePdfResponse(Guid id) => new(id);
    }
}
