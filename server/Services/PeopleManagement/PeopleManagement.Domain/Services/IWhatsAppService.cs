namespace PeopleManagement.Domain.Services
{
    public interface IWhatsAppService
    {
        Task SendTextMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

        Task SendMediaMessageAsync(
            string phoneNumber, 
            string mediaType, 
            string mimeType, 
            string caption, 
            string media, 
            string fileName, 
            CancellationToken cancellationToken = default);
    }
}
