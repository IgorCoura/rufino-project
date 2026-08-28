namespace BillPayment.Application.Models.Notifications;

using BillPayment.Application.Notifications.Commands;

/// <summary>
/// Corpo da configuração de avisos.
/// </summary>
/// <remarks>
/// O <c>tenantId</c> não entra aqui: ele vem da rota, e aceitá-lo no corpo permitiria configurar
/// aviso em nome de outra conta — o vetor de IDOR que todo Model do BC fecha.
/// </remarks>
public sealed record ConfigureTenantNotificationsModel(
    IReadOnlyCollection<string> Recipients,
    bool IsEnabled)
{
    public ConfigureTenantNotificationsCommand ToCommand(Guid tenantId)
        => new(tenantId, Recipients ?? [], IsEnabled);
}
