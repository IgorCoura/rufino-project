namespace PeopleManagement.Domain.Services
{
    public interface IWhatsAppService
    {
        Task SendTextMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
    }
}
