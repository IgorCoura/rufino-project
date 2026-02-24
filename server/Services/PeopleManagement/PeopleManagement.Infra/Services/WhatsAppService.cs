using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PeopleManagement.Domain.Options;
using PeopleManagement.Domain.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace PeopleManagement.Infra.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly WhatsAppOptions _options;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(
            HttpClient httpClient,
            IOptions<WhatsAppOptions> options,
            ILogger<WhatsAppService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendTextMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/message/sendText/{_options.Instance}";
                var fullUrl = $"{_httpClient.BaseAddress}{url.TrimStart('/')}";

                var requestBody = new
                {
                    number = phoneNumber,
                    text = message,
                };

                var bodyJson = JsonSerializer.Serialize(requestBody);
                var apiKeyMasked = _options.ApiKey;

                _logger.LogInformation(
                    "Sending WhatsApp message - URL: {FullUrl}, Instance: {Instance}, ApiKey: {ApiKey}, Body: {Body}",
                    fullUrl,
                    _options.Instance,
                    apiKeyMasked,
                    bodyJson);

                var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "WhatsApp API returned error - StatusCode: {StatusCode}, Response: {ErrorContent}",
                        response.StatusCode,
                        errorContent);
                }

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("WhatsApp message sent successfully to {PhoneNumber}. Response: {Response}", 
                    phoneNumber, responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp message to {PhoneNumber}. Message {Menssage}", phoneNumber, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending WhatsApp message to {PhoneNumber}", phoneNumber);
                throw;
            }
        }

        public async Task SendMediaMessageAsync(
            string phoneNumber, 
            string mediaType, 
            string mimeType, 
            string caption, 
            string media, 
            string fileName, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/message/sendMedia/{_options.Instance}";

                var requestBody = new
                {
                    number = phoneNumber,
                    mediatype = mediaType,
                    mimetype = mimeType,
                    caption = caption,
                    media = media,
                    fileName = fileName
                };

                _logger.LogInformation("Sending WhatsApp media message to {PhoneNumber} with type {MediaType}", 
                    phoneNumber, mediaType);

                var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("WhatsApp media message sent successfully to {PhoneNumber}. Response: {Response}", 
                    phoneNumber, responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp media message to {PhoneNumber}", phoneNumber);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending WhatsApp media message to {PhoneNumber}", phoneNumber);
                throw;
            }
        }
    }
}
