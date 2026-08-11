namespace BillPayment.Infra.Mailboxes.Graph;

using System.Collections.Concurrent;
using System.Text.Json;
using BillPayment.Domain.Mailboxes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

/// <summary>
/// Obtém e reaproveita o token de aplicativo (<em>client credentials</em>) de cada fonte.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O cache não é otimização, é necessidade.</strong> Sem ele, cada varredura de cada
/// fonte pediria um token novo, e o Entra ID limita a taxa desse endpoint — a captura passaria a
/// falhar por throttling da autenticação, não por problema na caixa.
/// </para>
/// <para>
/// A chave do cache <strong>não inclui o segredo</strong> (ver <c>GraphMailboxCredential.CacheKey</c>).
/// Guardar segredo em chave de dicionário o espalharia por dumps de memória e por qualquer log
/// de diagnóstico que imprimisse o cache.
/// </para>
/// </remarks>
internal sealed class GraphTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<GraphOptions> options,
    TimeProvider clock,
    ILogger<GraphTokenProvider> logger)
{
    /// <summary>
    /// Margem antes da expiração real. Um token que vence no meio de uma varredura de várias
    /// páginas derrubaria a varredura inteira sem cursor a guardar.
    /// </summary>
    private static readonly TimeSpan ExpirationSkew = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, CachedToken> _cache = new(StringComparer.Ordinal);
    private readonly GraphOptions _options = options.Value;

    public async Task<(string? Token, GraphFailure? Failure)> AcquireAsync(
        GraphMailboxCredential credential,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        if (_cache.TryGetValue(credential.CacheKey, out var cached) && cached.ExpiresAt > now)
            return (cached.Token, null);

        var (token, failure) = await RequestAsync(credential, cancellationToken);

        if (token is null)
            return (null, failure);

        _cache[credential.CacheKey] = token;
        return (token.Token, null);
    }

    private async Task<(CachedToken? Token, GraphFailure? Failure)> RequestAsync(
        GraphMailboxCredential credential,
        CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient(GraphHttp.TOKEN_CLIENT_NAME);
        var url = $"{_options.LoginUrl.TrimEnd('/')}/{credential.DirectoryId}/oauth2/v2.0/token";

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["client_id"] = credential.ClientId,
            ["client_secret"] = credential.ClientSecret,
            ["scope"] = "https://graph.microsoft.com/.default",
            ["grant_type"] = "client_credentials",
        });

        try
        {
            using var response = await http.PostAsync(new Uri(url), form, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Segredo errado, expirado ou app inexistente: nada disso melhora com espera.
                var status = (int)response.StatusCode is >= 500 or 429
                    ? MailboxStatus.Unavailable
                    : MailboxStatus.Denied;

                logger.LogWarning("Entra ID recusou o token do aplicativo {ClientId}: {Status}",
                    credential.ClientId, (int)response.StatusCode);

                return (null, new GraphFailure(status, "token_request_failed", null));
            }

            var parsed = JsonSerializer.Deserialize<GraphTokenResponse>(content, GraphHttp.Json);

            if (parsed?.AccessToken is null)
                return (null, new GraphFailure(MailboxStatus.Unavailable, "token_response_malformed", null));

            var lifetime = TimeSpan.FromSeconds(parsed.ExpiresIn ?? 3600);
            var expiresAt = clock.GetUtcNow() + (lifetime > ExpirationSkew ? lifetime - ExpirationSkew : lifetime);

            return (new CachedToken(parsed.AccessToken, expiresAt), null);
        }
        catch (JsonException)
        {
            return (null, new GraphFailure(MailboxStatus.Unavailable, "token_response_malformed", null));
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Entra ID não respondeu ao pedido de token");
            return (null, new GraphFailure(MailboxStatus.Unavailable, "token_transport_error", ex.Message));
        }
    }

    private static bool IsTransport(Exception ex, CancellationToken cancellationToken)
        => ex switch
        {
            OperationCanceledException => !cancellationToken.IsCancellationRequested,
            HttpRequestException or TimeoutRejectedException or BrokenCircuitException => true,
            _ => false,
        };

    private sealed record CachedToken(string Token, DateTimeOffset ExpiresAt);
}
