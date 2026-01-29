namespace PeopleManagement.Application.Commands.DocumentCommands.ReceiveWebhookDocument
{
    public record ReceiveWebhookDocumentResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator ReceiveWebhookDocumentResponse(Guid Id) => new(Id);
    }
}
