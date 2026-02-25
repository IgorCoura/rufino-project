using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PeopleManagement.Domain.Options;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.Services
{
    public class WhatsAppHealthCheckService : IWhatsAppHealthCheckService
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<WhatsAppHealthCheckService> _logger;
        private readonly string _healthCheckNumber;
        private readonly TimeZoneInfo _timeZone;

        public WhatsAppHealthCheckService(
            IWhatsAppService whatsAppService,
            IOptions<WhatsAppOptions> options,
            IOptions<TimeZoneOptions> timeZoneOptions,
            ILogger<WhatsAppHealthCheckService> logger)
        {
            _whatsAppService = whatsAppService;
            _logger = logger;
            _healthCheckNumber = options.Value.HealthCheckNumber;
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneOptions.Value.TimeZoneId);
        }

        public async Task SendHealthCheckMessage(CancellationToken cancellationToken = default)
        {
            try
            {
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
                var message = $"WhatsApp está funcionando, {now:dd/MM/yyyy} às {now:HH:mm}h.";

                _logger.LogInformation(
                    "Sending WhatsApp health check message to {PhoneNumber} at {DateTime}",
                    _healthCheckNumber,
                    now);

                await _whatsAppService.SendTextMessageAsync(_healthCheckNumber, message, cancellationToken);

                _logger.LogInformation(
                    "WhatsApp health check message sent successfully to {PhoneNumber}",
                    _healthCheckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending WhatsApp health check message to {PhoneNumber}",
                    _healthCheckNumber);
            }
        }
    }
}
