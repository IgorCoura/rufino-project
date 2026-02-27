using Hangfire;
using Microsoft.Extensions.Options;
using PeopleManagement.Domain.Options;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.Services
{
    public class WhatsAppQueueService : IWhatsAppQueueService
    {
        private readonly WhatsAppQueueOptions _options;

        public WhatsAppQueueService(IOptions<WhatsAppQueueOptions> options)
        {
            _options = options.Value;
        }

        public string EnqueueTextMessage(string phoneNumber, string message)
        {
            var effectiveDelay = Math.Max(0, _options.DelaySeconds);

            return BackgroundJob.Schedule<IWhatsAppService>(
                _options.QueueName,
                service => service.SendTextMessageAsync(phoneNumber, message, CancellationToken.None),
                TimeSpan.FromSeconds(effectiveDelay));
        }

        public string EnqueueMediaMessage(
            string phoneNumber,
            string mediaType,
            string mimeType,
            string caption,
            string media,
            string fileName)
        {
            var effectiveDelay = Math.Max(0, _options.DelaySeconds);

            return BackgroundJob.Schedule<IWhatsAppService>(
                _options.QueueName,
                service => service.SendMediaMessageAsync(
                    phoneNumber,
                    mediaType,
                    mimeType,
                    caption,
                    media,
                    fileName,
                    CancellationToken.None),
                TimeSpan.FromSeconds(effectiveDelay));
        }
    }
}
