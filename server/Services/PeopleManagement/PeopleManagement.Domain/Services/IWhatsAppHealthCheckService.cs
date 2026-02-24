namespace PeopleManagement.Domain.Services
{
    public interface IWhatsAppHealthCheckService
    {
        Task SendHealthCheckMessage(CancellationToken cancellationToken = default);
    }
}
