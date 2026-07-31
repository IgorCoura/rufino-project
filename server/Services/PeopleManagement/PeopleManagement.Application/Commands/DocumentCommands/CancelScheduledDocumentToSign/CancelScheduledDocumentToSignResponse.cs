namespace PeopleManagement.Application.Commands.DocumentCommands.CancelScheduledDocumentToSign
{
    public record CancelScheduledDocumentToSignResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CancelScheduledDocumentToSignResponse(Guid id) => new(id);
    }
}
