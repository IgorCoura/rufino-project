namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;

/// <summary>
/// O ternário de trilho num lugar só: qual gateway fala por esta ordem. Antes ele estava copiado
/// em seis handlers — e cancelamento via provedor existia em três, cada um por sua conta; um
/// trilho novo (ou um esquecimento num deles) faria os sites divergirem em silêncio.
/// </summary>
internal static class PaymentGatewayRouting
{
    public static Task<PaymentFetchResult> FindByExternalReferenceAsync(
        this PaymentOrder order,
        IBillPaymentGateway billGateway,
        IPixPaymentGateway pixGateway,
        CredentialRef? credential,
        CancellationToken cancellationToken)
        => order.Rail == PaymentRail.Pix
            ? pixGateway.FindByExternalReferenceAsync(credential, order.ExternalReference, cancellationToken)
            : billGateway.FindByExternalReferenceAsync(credential, order.ExternalReference, cancellationToken);

    public static Task<PaymentFetchResult> GetFromProviderAsync(
        this PaymentOrder order,
        IBillPaymentGateway billGateway,
        IPixPaymentGateway pixGateway,
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken)
        => order.Rail == PaymentRail.Pix
            ? pixGateway.GetAsync(credential, providerOrderId, cancellationToken)
            : billGateway.GetAsync(credential, providerOrderId, cancellationToken);

    public static Task<PaymentCancellationResult> CancelAtProviderAsync(
        this PaymentOrder order,
        IBillPaymentGateway billGateway,
        IPixPaymentGateway pixGateway,
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken)
        => order.Rail == PaymentRail.Pix
            ? pixGateway.CancelAsync(credential, providerOrderId, cancellationToken)
            : billGateway.CancelAsync(credential, providerOrderId, cancellationToken);
}
