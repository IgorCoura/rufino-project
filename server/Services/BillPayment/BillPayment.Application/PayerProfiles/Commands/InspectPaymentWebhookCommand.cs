namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Options;

/// <summary>
/// O estado do webhook deste tenant, como esta instalação o guarda e como o provedor o reporta.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Não é uma Query</strong> apesar de não mutar nada: neste BC "Query" significa leitura
/// direta do banco com <c>AsNoTracking</c> e sem mediator, e isto aqui faz uma chamada HTTP ao
/// provedor. Passa pelo mediator como qualquer operação que fala com o mundo externo.
/// </para>
/// <para>
/// <strong>Nunca devolve o token</strong>, nem o ponteiro dele no cofre — só o fato de existir.
/// A tela precisa saber se o webhook está entregando, não com que segredo.
/// </para>
/// </remarks>
public sealed record InspectPaymentWebhookCommand(Guid TenantId)
    : ITenantScopedCommand, IRequest<InspectPaymentWebhookResponse>;

/// <param name="Configured">Há webhook provisionado do lado de cá.</param>
/// <param name="Delivering">
/// O provedor confirma que ele existe, está habilitado, com a fila corrente e apontando para
/// <paramref name="ExpectedUrl"/>. É a única linha que responde "está funcionando?".
/// </param>
/// <param name="ProviderReachable">
/// Deu para perguntar ao provedor. Falso aqui invalida tudo o que vem dele nesta resposta — e
/// não significa webhook quebrado.
/// </param>
public sealed record InspectPaymentWebhookResponse(
    bool AccountLinked,
    bool Configured,
    bool Delivering,
    bool ProviderReachable,
    string? ProviderWebhookId,
    string? ExpectedUrl,
    string? ProviderUrl,
    bool? Enabled,
    bool? Interrupted,
    int? PenalizedRequestsCount,
    DateTime? LastEventAt,
    DateTime? TokenRotatedAt,
    string? ReasonCode);

public sealed class InspectPaymentWebhookCommandHandler(
    IPayerProfileRepository payerProfiles,
    IPaymentWebhookProvisioner provisioner,
    IOptions<PaymentWebhookOptions> options)
    : IRequestHandler<InspectPaymentWebhookCommand, InspectPaymentWebhookResponse>
{
    public async Task<InspectPaymentWebhookResponse> Handle(
        InspectPaymentWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        var settings = options.Value;
        var expectedUrl = string.IsNullOrWhiteSpace(settings.PublicBaseUrl)
            ? null
            : settings.BuildCallbackUrl(request.TenantId);

        if (!profile.CanSchedulePayments || !profile.HasWebhook)
        {
            return new InspectPaymentWebhookResponse(
                AccountLinked: profile.CanSchedulePayments,
                Configured: profile.HasWebhook,
                Delivering: false,
                ProviderReachable: false,
                ProviderWebhookId: profile.AsaasWebhookId,
                ExpectedUrl: expectedUrl,
                ProviderUrl: null,
                Enabled: null,
                Interrupted: null,
                PenalizedRequestsCount: null,
                LastEventAt: profile.LastWebhookEventAt,
                TokenRotatedAt: profile.AsaasWebhookRotatedAt,
                ReasonCode: expectedUrl is null ? "public_base_url_missing" : null);
        }

        var health = await provisioner.GetHealthAsync(
            profile.AsaasAccountRef, profile.AsaasWebhookId!, cancellationToken);

        return new InspectPaymentWebhookResponse(
            AccountLinked: true,
            Configured: true,
            Delivering: expectedUrl is not null && health.IsDelivering(expectedUrl),
            ProviderReachable: !health.IsUnavailable,
            ProviderWebhookId: profile.AsaasWebhookId,
            ExpectedUrl: expectedUrl,
            ProviderUrl: health.Url,
            Enabled: health.Found ? health.Enabled : null,
            Interrupted: health.Found ? health.Interrupted : null,
            PenalizedRequestsCount: health.Found ? health.PenalizedRequestsCount : null,
            LastEventAt: profile.LastWebhookEventAt,
            TokenRotatedAt: profile.AsaasWebhookRotatedAt,
            ReasonCode: health.ReasonCode ?? (health.Found ? null : "not_found_at_provider"));
    }
}
