namespace BillPayment.Application.Models.Retention;

using BillPayment.Application.Retention.Commands;

/// <summary>
/// Corpo do PUT da política de retenção.
/// </summary>
/// <remarks>
/// O <c>TenantId</c> vem da rota e <strong>não</strong> do corpo — aceitar do corpo é vetor de
/// IDOR, e é por isso que todo Model deste BC expõe <c>ToCommand(tenantId, ...)</c> em vez de o
/// controller montar o Command com <c>new</c>.
/// </remarks>
/// <param name="WindowDays">7, 30, 90 ou 180. Quem recusa o resto é o domínio.</param>
public sealed record ConfigureCaptureRetentionModel(bool IsEnabled, int WindowDays)
{
    public ConfigureCaptureRetentionCommand ToCommand(Guid tenantId)
        => new(tenantId, IsEnabled, WindowDays);
}
