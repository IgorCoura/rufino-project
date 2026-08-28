namespace BillPayment.Infra.Notifications;

using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tenta o canal externo e cai no log quando ele falha. <strong>Nunca propaga exceção.</strong>
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o que sustenta a regra do ADR-014 de que falha de envio não desfaz o alerta.</strong>
/// O registro do alerta vive no agregado e é gravado na transação do efeito; o envio acontece
/// depois, pelo outbox. Se uma falha de envio subisse, o handler morreria, o outbox reentregaria
/// o mesmo evento indefinidamente, e o alerta sairia repetido no dia em que o canal voltasse —
/// que é exatamente como se treina alguém a ignorar alerta.
/// </para>
/// <para>
/// <strong>O log não é consolo, é o canal que sempre funciona</strong>, ao lado de
/// <c>GET /expectations/pending</c>. Por isso o substituto entra <em>sempre</em>, e não só quando
/// o principal está desconfigurado: um aviso que o provedor recusou continua registrado em algum
/// lugar legível.
/// </para>
/// <para>
/// Compare com <c>UnconfiguredAttachmentStorage</c>, que faz o oposto e falha alto: guardar
/// arquivo em lugar nenhum sem avisar perde um comprovante que ninguém recupera. Aviso não
/// entregue não perde nada — o fato continua no agregado.
/// </para>
/// </remarks>
internal sealed class ResilientNotificationSender(
    GraphNotificationSender primary,
    LoggingNotificationSender fallback,
    ILogger<ResilientNotificationSender> logger) : INotificationSender
{
    public async Task SendAsync(
        TenantId tenantId,
        NotificationKind kind,
        NotificationPayload payload,
        CancellationToken cancellationToken)
    {
        // O log recebe TODOS os avisos, inclusive os entregues: é a trilha de que o alerta saiu.
        await fallback.SendAsync(tenantId, kind, payload, cancellationToken);

        try
        {
            await primary.SendAsync(tenantId, kind, payload, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex, "Canal externo de aviso falhou; o alerta segue registrado no agregado.");
        }
    }
}
