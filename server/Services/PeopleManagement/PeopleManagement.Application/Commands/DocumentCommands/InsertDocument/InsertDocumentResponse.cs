using PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf;

namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocument
{
    public record InsertDocumentResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator InsertDocumentResponse(Guid id) => new(id);
    }
}
