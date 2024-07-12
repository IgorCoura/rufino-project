using PeopleManagement.Application.Commands.SecurityDocumentCommands.GeneratePdf;

namespace PeopleManagement.Application.Commands.SecurityDocumentCommands.InsertDocument
{
    public record InsertDocumentResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator InsertDocumentResponse(Guid id) => new(id);
    }
}
