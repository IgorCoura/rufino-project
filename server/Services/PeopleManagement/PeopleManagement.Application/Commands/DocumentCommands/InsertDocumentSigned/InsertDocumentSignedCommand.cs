using System.Text.Json.Nodes;

namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentSigned
{
    public record InsertDocumentSignedCommand(JsonNode ContentBody) : IRequest<InsertDocumentSignedResponse> 
    {
    }

}
