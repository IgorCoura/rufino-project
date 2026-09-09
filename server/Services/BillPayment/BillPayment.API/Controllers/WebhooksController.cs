namespace BillPayment.API.Controllers;

using System.Text.Json;
using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// O ponto de entrada dos webhooks do provedor de pagamento — <strong>um por tenant</strong>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Vive FORA de <c>api/v1</c> de propósito</strong>: o provedor não fala Keycloak, e toda
/// rota sob <c>api/v1</c> exige <c>[ProtectedResource]</c> por teste de erosão.
/// </para>
/// <para>
/// <strong>O tenant vem na URL; o token prova quem é.</strong> Cada conta Asaas recebe, no
/// provisionamento, a sua própria URL (<c>/webhooks/asaas/{tenantId}</c>) e o seu próprio
/// <c>authToken</c>, guardado cifrado no cofre. O token da INSTALAÇÃO deixou de existir em
/// 2026-09-08: com uma conta por tenant (ADR-016), um segredo compartilhado deixaria qualquer
/// tenant forjar evento de qualquer outro.
/// </para>
/// <para>
/// <strong>O payload é um AVISO, não uma verdade.</strong> O provedor não assina o corpo (só
/// oferece o token no header, sem HMAC), então quem obtivesse o token poderia forjar qualquer
/// conteúdo. Por isso o handler <strong>relê a ordem no provedor</strong> com a chave do tenant e
/// aplica o que a leitura afirmar — um corpo forjado custa, no pior caso, uma chamada de API.
/// </para>
/// <para>
/// <strong>Responder não-2xx represa a fila SEQUENCIAL de webhooks da conta inteira</strong>, e
/// o provedor a interrompe depois de falhas repetidas (o <c>penalizedRequestsCount</c> que a
/// varredura de saúde vigia). Por isso evento desconhecido, fora de ordem ou de tenant sem
/// webhook devolve 200 com desfecho nomeado — nunca erro.
/// </para>
/// </remarks>
[AllowAnonymous]
[Route("webhooks/asaas")]
public sealed class WebhooksController(
    IMediator mediator,
    ILogger<WebhooksController> logger) : BaseController(logger)
{
    [HttpPost("{tenantId:guid}")]
    public async Task<IActionResult> ReceiveEvent(
        [FromRoute] Guid tenantId,
        [FromHeader(Name = "asaas-access-token")] string? token,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        var parsed = TryParse(body);
        if (parsed is null)
            return BadRequest();

        var response = await mediator.Send(
            parsed with { TenantId = tenantId, PresentedToken = token ?? string.Empty },
            cancellationToken);

        // 401 é o ÚNICO não-2xx daqui, e é deliberado: token errado não é evento nosso para
        // absorver — represar a fila de quem está tentando forjar é o comportamento certo.
        return response.Outcome == ProcessAsaasWebhookCommandHandler.OUTCOME_UNAUTHORIZED
            ? Unauthorized()
            : Ok(new { outcome = response.Outcome });
    }

    /// <summary>
    /// Lê o payload com frouxidão deliberada. O objeto muda de nome conforme a família do evento:
    /// <c>bill</c> no pague-contas e <c>transfer</c> na saída de Pix — <strong>medido em sandbox
    /// (2026-09-08)</strong>, onde se confirmou que não existe evento <c>PIX_TRANSACTION_*</c> e
    /// que toda saída de Pix é notificada como <c>TRANSFER_*</c>.
    /// </summary>
    private static ProcessAsaasWebhookCommand? TryParse(JsonElement root)
    {
        var eventId = ReadString(root, "id");
        var eventName = ReadString(root, "event");

        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(eventName))
            return null;

        JsonElement payload = default;
        var hasPayload =
            root.TryGetProperty("bill", out payload)
            || root.TryGetProperty("transfer", out payload)
            || root.TryGetProperty("pixTransaction", out payload)
            || root.TryGetProperty("payment", out payload);

        string? externalReference = null;
        string? providerObjectId = null;
        string? payloadStatus = null;

        if (hasPayload && payload.ValueKind == JsonValueKind.Object)
        {
            externalReference = ReadString(payload, "externalReference");
            providerObjectId = ReadString(payload, "id");
            payloadStatus = ReadString(payload, "status");
        }

        return new ProcessAsaasWebhookCommand(
            Guid.Empty,
            string.Empty,
            eventId!,
            eventName!,
            externalReference,
            providerObjectId,
            payloadStatus);
    }

    private static string? ReadString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
