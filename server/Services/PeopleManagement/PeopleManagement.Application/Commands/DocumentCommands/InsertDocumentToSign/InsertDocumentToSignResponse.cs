namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign
{
    public record InsertDocumentToSignResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator InsertDocumentToSignResponse(Guid id) => new(id);
    }
}
