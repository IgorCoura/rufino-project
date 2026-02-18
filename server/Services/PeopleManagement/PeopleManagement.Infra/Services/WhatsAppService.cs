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

                var requestBody = new
                {
                    number = phoneNumber,
                    text = message,
                };

                _logger.LogInformation("Sending WhatsApp message to {PhoneNumber}", phoneNumber);

                var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("WhatsApp message sent successfully to {PhoneNumber}. Response: {Response}", 
                    phoneNumber, responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp message to {PhoneNumber}", phoneNumber);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending WhatsApp message to {PhoneNumber}", phoneNumber);
                throw;
            }
        }
    }
}
