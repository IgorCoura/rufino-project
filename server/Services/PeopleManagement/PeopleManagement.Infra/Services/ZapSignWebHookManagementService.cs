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

namespace PeopleManagement.Infra.Services
{
    public class ZapSignWebHookManagementService : IWebHookManagementService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ZapSignWebHookManagementService> _logger;
        private readonly IAuthorizationService _authorizationService;
        private readonly SignOptions _signOptions;
        private readonly IWebHookRepository _webHookRepository;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public ZapSignWebHookManagementService(
            HttpClient httpClient,
            ILogger<ZapSignWebHookManagementService> logger,
            IAuthorizationService authorizationService,
            SignOptions signOptions,
            IWebHookRepository webHookRepository)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authorizationService = authorizationService;
            _signOptions = signOptions;
            _webHookRepository = webHookRepository;
        }

        public async Task<string?> CreateWebHookEvent(CancellationToken cancellationToken = default)
        {
            try
            {
                var authorizationToken = await _authorizationService.GetAuthorizationToken();

                var body = new JsonObject
                {
                    ["url"] = _signOptions.WeebHookUrl,
                    ["type"] = "",
                    ["headers"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "Authorization",
                            ["value"] = $"Bearer {authorizationToken}"
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync("/api/v1/user/company/webhook", body, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Failed to create webhook. Url: {Url}, StatusCode: {StatusCode}, Response: {Response}",
                        _signOptions.WeebHookUrl, response.StatusCode, content);
                    return null;
                }

                var json = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
                return json?["id"]?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating webhook. Url: {Url}", _signOptions.WeebHookUrl);
                return null;
            }
        }

        public async Task<bool> DeleteWebHook(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new JsonObject
                {
                    ["id"] = id
                };

                var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/user/company/webhook/delete/")
                {
                    Content = new StringContent(body.ToString(), System.Text.Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request, cancellationToken);

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

        [DisableConcurrentExecution(timeoutInSeconds: 900)] // 15 minutos de timeout
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 600 })] // Retry: 5min, 10min
        public async Task RefreshWebHookEvent(CancellationToken cancellationToken = default)
        {
            string? oldWebHookId = null;
            string? newWebHookId = null;

            await _semaphore.WaitAsync();
            try
            {
                var webHook = await _webHookRepository.FirstOrDefaultAsync(
                    x => x.Event == WebHookEvent.AllEvents,
                    cancellation: cancellationToken);

                if (webHook is not null && !string.IsNullOrEmpty(webHook.WebHookId))
                {
                    oldWebHookId = webHook.WebHookId;
                }

                // 1. CRIA O NOVO PRIMEIRO (garante que sempre haverá um webhook)
                newWebHookId = await CreateWebHookEvent(cancellationToken);

                if (string.IsNullOrEmpty(newWebHookId))
                {
                    _logger.LogError("Failed to create webhook. Keeping old webhook active. Old Id: {OldId}", oldWebHookId);
                    return; // Mantém o webhook antigo ativo
                }

                _logger.LogInformation("New webhook created successfully. Id: {NewId}", newWebHookId);

                // 2. TENTA SALVAR NO BANCO
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

                    // 3. SÓ DELETA O ANTIGO APÓS CONFIRMAR PERSISTÊNCIA
                    if (!string.IsNullOrEmpty(oldWebHookId) && oldWebHookId != newWebHookId)
                    {
                        _logger.LogInformation("Deleting old webhook. Id: {OldId}", oldWebHookId);
                        var deleted = await DeleteWebHook(oldWebHookId, cancellationToken);

                        if (!deleted)
                        {
                            _logger.LogWarning("Failed to delete old webhook. It may need manual cleanup. Old Id: {OldId}", oldWebHookId);
                            // Não é crítico - o webhook antigo vai expirar naturalmente
                        }
                    }

                    _logger.LogInformation("Webhook refreshed successfully. New Id: {NewId}", newWebHookId);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Failed to save webhook to database. Rolling back new webhook. Id: {WebHookId}", newWebHookId);

                    // ROLLBACK: Deleta o webhook recém-criado
                    await DeleteWebHook(newWebHookId, cancellationToken);

                    // Não propaga a exceção - o webhook antigo continua ativo
                    _logger.LogInformation("Rollback completed. Old webhook remains active. Old Id: {OldId}", oldWebHookId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing webhook. Old webhook remains active if exists. Old Id: {OldId}", oldWebHookId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<WebhookDocumentEventModel?> ParseWebhookEvent(JsonNode contentBody, CancellationToken cancellationToken = default)
        {
            var status = contentBody?["event_type"]?.ToString() ?? "";
            var documentUnitId = contentBody?["external_id"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(documentUnitId) || !Guid.TryParse(documentUnitId, out var parsedDocumentUnitId))
            {
                _logger.LogWarning("Invalid or missing document unit ID in webhook payload");
                throw new DomainException(this, InfraErrors.SignDoc.ErrorInRecoverDocSigned(documentUnitId));
            }

            var webhookStatus = status switch
            {
                "doc_created" => WebhookDocumentStatus.DocCreated,
                "doc_signed" => WebhookDocumentStatus.DocSigned,
                "doc_refused" => WebhookDocumentStatus.DocRefused,
                "doc_deleted" => WebhookDocumentStatus.DocDeleted,
                "doc_expired" => WebhookDocumentStatus.DocExpired,
                _ => WebhookDocumentStatus.Unknown
            };

            var documentUrl = contentBody?["signed_file"]?.ToString() ?? "";

            return new WebhookDocumentEventModel(parsedDocumentUnitId, webhookStatus, documentUrl);
        }
    }
}
