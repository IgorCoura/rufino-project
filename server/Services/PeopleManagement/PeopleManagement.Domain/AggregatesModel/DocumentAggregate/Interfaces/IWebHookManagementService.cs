using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;
using System.Text.Json.Nodes;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    public interface IWebHookManagementService
    {
        Task<string?> CreateWebHookEvent(CancellationToken cancellationToken = default);
        Task<bool> DeleteWebHook(string id, CancellationToken cancellationToken = default);
        Task RefreshWebHookEvent(CancellationToken cancellationToken = default);
        Task<WebhookDocumentEventModel?> ParseWebhookEvent(JsonNode contentBody, CancellationToken cancellationToken = default);
    }
}
