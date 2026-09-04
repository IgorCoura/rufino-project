namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
using BillPayment.Application.Mediator;
using BillPayment.Application.Models.Notifications;
using BillPayment.Application.Notifications.Commands;
using BillPayment.Application.Queries.Notifications;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Para quem os avisos de expectativa vão.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Sem isto o canal de aviso não tem destinatário.</strong> A porta
/// <c>INotificationSender</c> sempre recebeu o tenant, e o BC não guardava nenhum contato — o
/// único e-mail existente era o da <c>CaptureSource</c>, que é a caixa <em>de captura</em> e não
/// a caixa de uma pessoa. Era esta a lacuna que fazia o alerta do ADR-014 só existir no painel.
/// </para>
/// <para>
/// Fica sob o recurso <c>expectation</c>, e não sob um recurso novo, de propósito: escopo novo
/// exige <em>partial import</em> no realm, que é passo de deploy e já é pendência do checklist.
/// Quem configura para onde o aviso vai é quem administra as expectativas.
/// </para>
/// </remarks>
[Route("api/v1/{tenantId:guid}/notification-settings")]
public sealed class NotificationSettingsController(
    IMediator mediator,
    ITenantNotificationQueries queries,
    ILogger<NotificationSettingsController> logger) : BaseController(logger)
{
    /// <summary>
    /// A configuração atual. Tenant que nunca configurou devolve lista vazia e envio desligado —
    /// não 404: é o mesmo estado de quem desligou, e distinguir os dois só duplicaria a tradução
    /// no cliente.
    /// </summary>
    [HttpGet]
    [ProtectedResource("expectation", "view")]
    public async Task<ActionResult<TenantNotificationSettingsDto>> Get(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken)
        => OkResponse(await queries.GetAsync(tenantId, cancellationToken));

    /// <summary>
    /// Define a lista de destinatários e liga ou desliga o envio externo.
    /// </summary>
    /// <remarks>
    /// <strong>Substitui a lista inteira</strong> — quem edita vê tudo e devolve tudo. E
    /// desligar aqui não apaga alerta nenhum: o registro vive no agregado da expectativa e
    /// continua em <c>GET /expectations/pending</c>, que é o canal que funciona sem configuração.
    /// </remarks>
    [HttpPut]
    [ProtectedResource("expectation", "manage")]
    public async Task<ActionResult<ConfigureTenantNotificationsResponse>> Configure(
        [FromRoute] Guid tenantId,
        [FromBody] ConfigureTenantNotificationsModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<ConfigureTenantNotificationsCommand, ConfigureTenantNotificationsResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }
}
