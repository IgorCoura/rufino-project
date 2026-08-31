namespace BillPayment.Infra.Asaas;

using BillPayment.Domain.Ports;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

/// <summary>
/// Prova a chave da subconta com <c>GET /v3/myAccount/</c> — read-only e barato, a mesma
/// doutrina da prova de acesso à caixa: chave errada é recusada na hora do cadastro.
/// </summary>
/// <remarks>
/// <strong>Aqui 401/403 é RECUSA, ao contrário da classificação da consulta</strong> (onde 403
/// costuma ser limite de taxa disfarçado e é retentável): a pergunta desta chamada é
/// literalmente "esta chave funciona?", e o provedor respondendo "não autorizado" é a resposta,
/// não uma instabilidade. A chave nunca entra em log — só status e motivo.
/// </remarks>
internal sealed class AsaasAccountVerifier(
    IHttpClientFactory httpClientFactory,
    ILogger<AsaasAccountVerifier> logger) : IPaymentAccountVerifier
{
    private const string PROBE_PATH = "myAccount/";

    public async Task<PaymentAccountProbe> ProbeAsync(string apiKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        using var client = httpClientFactory.CreateClient(AsaasHttp.LOOKUP_CLIENT_NAME);
        client.DefaultRequestHeaders.Add("access_token", apiKey);

        try
        {
            using var response = await client.GetAsync(new Uri(PROBE_PATH, UriKind.Relative), cancellationToken);
            var status = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
                return PaymentAccountProbe.Ok();

            logger.LogWarning("A prova da chave Asaas respondeu {Status}", status);
            return status switch
            {
                401 or 403 => PaymentAccountProbe.Rejected("invalid_api_key"),
                408 or 429 => PaymentAccountProbe.Unavailable("rate_limited"),
                >= 500 => PaymentAccountProbe.Unavailable("provider_error"),
                _ => PaymentAccountProbe.Rejected($"http_{status}"),
            };
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "A prova da chave Asaas não obteve resposta");
            return PaymentAccountProbe.Unavailable(ex is BrokenCircuitException ? "circuit_open" : "transport_error");
        }
    }

    private static bool IsTransport(Exception ex, CancellationToken cancellationToken)
        => ex switch
        {
            OperationCanceledException => !cancellationToken.IsCancellationRequested,
            HttpRequestException or TimeoutRejectedException or BrokenCircuitException => true,
            _ => false,
        };
}
