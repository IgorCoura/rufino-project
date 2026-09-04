namespace BillPayment.Domain.Notifications;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. Toda busca filtra por <see cref="TenantId"/> — este repositório
/// não tem nenhuma das três travessias de tenant autorizadas do BC.
/// </summary>
public interface ITenantNotificationSettingsRepository
{
    Task AddAsync(TenantNotificationSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Um por tenant — sem id na assinatura.</summary>
    Task<TenantNotificationSettings?> GetAsync(
        TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leitura para o envio. <c>AsNoTracking</c>, porque quem chama é o adapter de aviso e ele
    /// não muta nada.
    /// </summary>
    Task<TenantNotificationSettings?> FindForDeliveryAsync(
        TenantId tenantId, CancellationToken cancellationToken = default);
}
