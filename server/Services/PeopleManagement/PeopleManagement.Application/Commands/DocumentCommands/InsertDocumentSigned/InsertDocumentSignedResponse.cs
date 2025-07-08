namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentSigned
{
    public record InsertDocumentSignedResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator InsertDocumentSignedResponse(Guid Id) => new(Id);
    }
}
