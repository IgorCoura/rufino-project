namespace PeopleManagement.Domain.Services
{
    public interface IWhatsAppQueueService
    {
        string EnqueueTextMessage(string phoneNumber, string message, int delaySeconds = 0);

        string EnqueueMediaMessage(
            string phoneNumber,
            string mediaType,
            string mimeType,
            string caption,
            string media,
            string fileName,
            int delaySeconds = 0);
    }
}
