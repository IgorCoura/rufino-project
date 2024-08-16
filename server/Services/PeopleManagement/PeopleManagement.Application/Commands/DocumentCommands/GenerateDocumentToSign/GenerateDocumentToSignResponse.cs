namespace PeopleManagement.Application.Commands.DocumentCommands.GenerateDocumentToSign
{
    public record GenerateDocumentToSignResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator GenerateDocumentToSignResponse(Guid id) => new(id);
    }
}
