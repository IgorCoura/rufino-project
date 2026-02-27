namespace PeopleManagement.Domain.Services
{
    public interface IWhatsAppQueueService
    {
        string EnqueueTextMessage(string phoneNumber, string message);

        string EnqueueMediaMessage(
            string phoneNumber,
            string mediaType,
            string mimeType,
            string caption,
            string media,
            string fileName);
    }
}
