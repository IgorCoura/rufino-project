using Hangfire;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.Services
{
    public class WhatsAppQueueService : IWhatsAppQueueService
    {
        private const string QueueName = "whatsapp";

        public string EnqueueTextMessage(string phoneNumber, string message, int delaySeconds = 0)
        {
            return BackgroundJob.Schedule<IWhatsAppService>(
                QueueName,
                service => service.SendTextMessageAsync(phoneNumber, message, CancellationToken.None),
                TimeSpan.FromSeconds(delaySeconds));
        }

        public string EnqueueMediaMessage(
            string phoneNumber,
            string mediaType,
            string mimeType,
            string caption,
            string media,
            string fileName,
            int delaySeconds = 0)
        {
            return BackgroundJob.Schedule<IWhatsAppService>(
                QueueName,
                service => service.SendMediaMessageAsync(
                    phoneNumber,
                    mediaType,
                    mimeType,
                    caption,
                    media,
                    fileName,
                    CancellationToken.None),
                TimeSpan.FromSeconds(delaySeconds));
        }
    }
}
