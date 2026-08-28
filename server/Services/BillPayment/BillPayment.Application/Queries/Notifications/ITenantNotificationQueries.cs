namespace BillPayment.Application.Queries.Notifications;

/// <param name="Recipients">Endereços já normalizados pelo domínio.</param>
public sealed record TenantNotificationSettingsDto(
    IReadOnlyCollection<string> Recipients,
    bool IsEnabled);

public interface ITenantNotificationQueries
{
    /// <summary>
    /// A configuração do tenant. <strong>Nunca devolve <c>null</c></strong>: o tenant que nunca
    /// configurou nada tem o mesmo estado de quem desligou — sem destinatário e sem envio —, e
    /// obrigar a tela a tratar 404 como "vazio" só duplicaria essa tradução no cliente.
    /// </summary>
    Task<TenantNotificationSettingsDto> GetAsync(
        Guid tenantId, CancellationToken cancellationToken = default);
}
