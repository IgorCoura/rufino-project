namespace BillPayment.API.Controllers;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

/// <summary>
/// O ponto de entrada dos webhooks do provedor de pagamento.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Vive FORA de <c>api/v1</c> de propósito</strong>: o provedor não conhece nosso
/// <c>tenantId</c>, e toda rota sob <c>api/v1</c> exige <c>{tenantId}</c> +
/// <c>[ProtectedResource]</c> por teste de erosão. Quem resolve o tenant é a
/// <c>externalReference</c> — que É o id da ordem (UC-14).
/// </para>
/// <para>
/// <c>[AllowAnonymous]</c> + token próprio: o provedor não fala Keycloak. O token é validado em
/// tempo constante, e <strong>sem token configurado o endpoint responde 404</strong> — um
/// webhook aberto seria um caminho anônimo para mexer em ordens de pagamento.
/// </para>
/// <para>
/// <strong>Ressalva registrada (ADR-016)</strong>: o desenho final é token POR TENANT,
/// provisionado com a chave dele — bloqueado junto com a sonda de sandbox. Até lá o token é da
/// instalação, por variável de ambiente, e a decisão está anotada no checklist.
/// </para>
/// </remarks>
[AllowAnonymous]
[Route("webhooks/asaas")]
public sealed class WebhooksController(
    IMediator mediator,
    IOptions<PaymentWebhookOptions> options,
    ILogger<WebhooksController> logger) : BaseController(logger)
{
    [HttpPost]
    public async Task<IActionResult> ReceiveBillEvent(
        [FromHeader(Name = "asaas-access-token")] string? token,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        var expected = options.Value.Token;
        if (string.IsNullOrWhiteSpace(expected))
            return NotFound();

        if (!FixedTimeEquals(token ?? string.Empty, expected))
            return Unauthorized();

        var command = TryParse(body);
        if (command is null)
            return BadRequest();

        var response = await mediator.Send(command, cancellationToken);
        return Ok(new { outcome = response.Outcome });
    }

    /// <summary>
    /// Lê o payload com frouxidão deliberada: o contrato do webhook é MEDIDO, não lido, e a
    /// sonda está bloqueada — o objeto pode chegar como <c>bill</c> ou <c>payment</c>.
    /// </summary>
    private static ProcessAsaasBillWebhookCommand? TryParse(JsonElement root)
    {
        var eventId = ReadString(root, "id");
        var eventName = ReadString(root, "event");

        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(eventName))
            return null;

        JsonElement payload = default;
        var hasPayload = root.TryGetProperty("bill", out payload) || root.TryGetProperty("payment", out payload);

        string? externalReference = null;
        DateOnly? paidAt = null;
        decimal? fee = null;
        var failReasons = new List<string>();

        if (hasPayload && payload.ValueKind == JsonValueKind.Object)
        {
            externalReference = ReadString(payload, "externalReference");

            if (DateOnly.TryParse(
                    ReadString(payload, "paymentDate"),
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsedDate))
            {
                paidAt = parsedDate;
            }

            if (payload.TryGetProperty("fee", out var feeElement) && feeElement.ValueKind == JsonValueKind.Number)
                fee = feeElement.GetDecimal();

            if (payload.TryGetProperty("failReasons", out var reasons) && reasons.ValueKind == JsonValueKind.Array)
            {
                failReasons.AddRange(reasons.EnumerateArray()
                    .Select(r => r.ValueKind == JsonValueKind.String ? r.GetString() : r.GetRawText())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r!));
            }
        }

        return new ProcessAsaasBillWebhookCommand(eventId!, eventName!, externalReference, paidAt, fee, failReasons);
    }

    private static string? ReadString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool FixedTimeEquals(string received, string expected)
    {
        var left = Encoding.UTF8.GetBytes(received);
        var right = Encoding.UTF8.GetBytes(expected);

        // Comprimentos diferentes já divergem; comparar mesmo assim mantém o tempo constante.
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}

/// <summary>O token do webhook — por variável de ambiente, nunca no appsettings (ADR-009).</summary>
public sealed class PaymentWebhookOptions
{
    public const string SectionName = "PaymentWebhook";

    public string? Token { get; set; }
}
