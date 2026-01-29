using System.Text.Json.Nodes;

namespace PeopleManagement.Application.Commands.DocumentCommands.ReceiveWebhookDocument
{
    public record ReceiveWebhookDocumentCommand(JsonNode ContentBody) : IRequest<ReceiveWebhookDocumentResponse> 
    {
    }

}
