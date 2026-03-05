using Hangfire;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options;
using PeopleManagement.Domain.AggregatesModel.WebHookAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace PeopleManagement.Infra.Services;

//REFERENCE API: https://developers.clicksign.com/reference
//DOC API: https://developers.clicksign.com/docs
public class ClickSignWebHookManagementService : IWebHookManagementService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClickSignWebHookManagementService> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly SignOptions _options;
    private readonly IWebHookRepository _webHookRepository;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ClickSignWebHookManagementService(
        HttpClient httpClient,
        ILogger<ClickSignWebHookManagementService> logger,
        IAuthorizationService authorizationService,
        SignOptions options,
        IWebHookRepository webHookRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authorizationService = authorizationService;
        _options = options;
        _webHookRepository = webHookRepository;
    }

    public async Task<string?> CreateWebHookEvent(CancellationToken cancellationToken = default)
    {
        try
        {
            var authorizationToken = await _authorizationService.GetAuthorizationToken();

            var body = new JsonObject
            {
                ["data"] = new JsonObject
                {
                    ["type"] = "hooks",
                    ["attributes"] = new JsonObject
                    {
                        ["url"] = _options.WebHookUrl,
                        ["content_type"] = "json",
                        ["secret"] = authorizationToken // HMAC secret para validação
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v3/hooks?access_token={_options.AccessToken}", body, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to create webhook. Url: {Url}, StatusCode: {StatusCode}, Response: {Response}",
                    _options.WebHookUrl, response.StatusCode, content);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
            return json?["data"]?["id"]?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook. Url: {Url}", _options.WebHookUrl);
            return null;
        }
    }

    public async Task<bool> DeleteWebHook(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/v3/hooks/{id}?access_token={_options.AccessToken}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to delete webhook. Id: {Id}, StatusCode: {StatusCode}, Response: {Response}",
                    id, response.StatusCode, content);
                return false;
            }

            _logger.LogInformation("Webhook deleted successfully. Id: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook. Id: {Id}", id);
            return false;
        }
    }

    [DisableConcurrentExecution(timeoutInSeconds: 900)]
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 600 })]
    public async Task RefreshWebHookEvent(CancellationToken cancellationToken = default)
    {
        string? oldWebHookId = null;
        string? newWebHookId = null;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var webHook = await _webHookRepository.FirstOrDefaultAsync(
                x => x.Event == WebHookEvent.AllEvents,
                cancellation: cancellationToken);

            if (webHook is not null && !string.IsNullOrEmpty(webHook.WebHookId))
                oldWebHookId = webHook.WebHookId;

            newWebHookId = await CreateWebHookEvent(cancellationToken);

            if (string.IsNullOrEmpty(newWebHookId))
            {
                _logger.LogError("Failed to create webhook. Keeping old webhook active. Old Id: {OldId}", oldWebHookId);
                return;
            }

            _logger.LogInformation("New webhook created. Id: {NewId}", newWebHookId);

            try
            {
                if (webHook is null)
                {
                    webHook = new WebHook(Guid.NewGuid(), newWebHookId, WebHookEvent.AllEvents);
                    await _webHookRepository.InsertAsync(webHook);
                }
                else
                {
                    webHook.WebHookId = newWebHookId;
                }

                await _webHookRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

                if (!string.IsNullOrEmpty(oldWebHookId) && oldWebHookId != newWebHookId)
                {
                    var deleted = await DeleteWebHook(oldWebHookId, cancellationToken);
                    if (!deleted)
                        _logger.LogWarning("Failed to delete old webhook. Manual cleanup may be needed. Old Id: {OldId}", oldWebHookId);
                }

                _logger.LogInformation("Webhook refreshed successfully. New Id: {NewId}", newWebHookId);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save webhook. Rolling back. Id: {WebHookId}", newWebHookId);
                await DeleteWebHook(newWebHookId, cancellationToken);
                _logger.LogInformation("Rollback completed. Old webhook remains active. Old Id: {OldId}", oldWebHookId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing webhook. Old Id: {OldId}", oldWebHookId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<WebhookDocumentEventModel?> ParseWebhookEvent(JsonNode contentBody, CancellationToken cancellationToken = default)
    {
        // ClickSign envia o evento no campo "event"
        var eventType = contentBody?["event"]?["name"]?.ToString() ?? "";
        var documentUnitId = contentBody?["document"]?["external_id"]?.ToString() ?? "";

        if (string.IsNullOrEmpty(documentUnitId) || !Guid.TryParse(documentUnitId, out var parsedDocumentUnitId))
        {
            _logger.LogWarning("Invalid or missing external_id in ClickSign webhook payload");
            throw new DomainException(this, InfraErrors.SignDoc.ErrorInRecoverDocSigned(documentUnitId));
        }

        // Mapeamento dos eventos ClickSign → WebhookDocumentStatus interno
        var status = eventType switch
        {
            "upload"           => WebhookDocumentStatus.DocCreated,
            "sign"             => WebhookDocumentStatus.DocSigned,
            "close"            => WebhookDocumentStatus.DocSigned,
            "document_closed"  => WebhookDocumentStatus.DocSigned,
            "refusal"          => WebhookDocumentStatus.DocRefused,
            "cancel"           => WebhookDocumentStatus.DocDeleted,
            "deadline"         => WebhookDocumentStatus.DocExpired,
            _                  => WebhookDocumentStatus.Unknown
        };

        // URL do documento assinado retornada no campo "document.downloads.signed_file_url"
        var documentUrl = contentBody?["document"]?["downloads"]?["signed_file_url"]?.ToString() ?? "";

        return Task.FromResult<WebhookDocumentEventModel?>(
            new WebhookDocumentEventModel(parsedDocumentUnitId, status, documentUrl));
    }
}