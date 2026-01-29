namespace PeopleManagement.Application.Commands.DocumentCommands.ReceiveWebhookDocument
{
    public record ReceiveWebhookDocumentResponse(string message) : BaseDTO(Guid.Empty)
    {
        public static implicit operator ReceiveWebhookDocumentResponse(string message) => new(message);
    }
}

