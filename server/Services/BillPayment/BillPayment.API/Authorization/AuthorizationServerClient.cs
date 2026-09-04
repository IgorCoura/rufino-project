namespace BillPayment.API.Authorization;

using System.Net;
using System.Text.Json;

/// <summary>
/// Fala com o servidor de autorização por ticket UMA, propagando o token de quem chamou.
/// </summary>
/// <remarks>
/// Desde 2026-09-04 tem UM método, e ele busca o retrato inteiro. Antes eram dois — um por
/// permissão (<c>VerifyAccessToResouce</c>) e um pela alçada de risco
/// (<c>GetGrantedScopesAsync</c>) —, o que fazia o <c>approve</c> ir duas vezes ao Keycloak na
/// mesma requisição e todo endpoint protegido ir uma. Quem resolve permissão agora é o
/// <see cref="RptSnapshot"/>, em memória; ver <see cref="RptCache"/>.
/// </remarks>
public class AuthorizationServerClient(HttpClient httpClient, AuthorizationOptions authorizationOptions) : IAuthorizationServerClient
{
    private readonly HttpClient _httpClient = httpClient;

    private readonly AuthorizationOptions _authorizationOptions = authorizationOptions;

    public async Task<RptFetchResult> FetchAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        // SEM o parâmetro "permission": é o que faz o Keycloak avaliar todas as permissões que a
        // pessoa alcança neste resource server, em vez de responder sobre uma só. O response_mode
        // tem que ser "permissions" — "decision" devolveria um RPT sem a lista, que é justamente o
        // que precisamos guardar.
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", _authorizationOptions.GrantType },
            { "response_mode", "permissions" },
            { "audience", _authorizationOptions.Resource },
        });

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(_authorizationOptions.TokenEndpointPath, content, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return RptFetchResult.Unavailable();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout do HttpClient chega como TaskCanceledException sem cancelamento pedido.
            // Sem este catch ele escaparia do handler de autorização e sairia como 500.
            return RptFetchResult.Unavailable();
        }

        using (response)
        {
            // O Keycloak responde 401 quando o token propagado é que foi recusado (expirado,
            // revogado, not-before) — isso é problema de AUTENTICAÇÃO, não permissão faltando,
            // e precisa chegar ao cliente como 401 para ele renovar o token.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return RptFetchResult.InvalidToken();

            if ((int)response.StatusCode >= 500)
                return RptFetchResult.Unavailable();

            // 403 aqui é o servidor dizendo "esta pessoa não alcança nada" — retrato VAZIO, não
            // falha. Tratá-lo como indisponibilidade faria quem não tem permissão receber 503, e
            // pior: com fail-static, herdar o retrato anterior de outra sessão.
            if (!response.IsSuccessStatusCode)
                return RptFetchResult.Resolved(RptSnapshot.Empty);

            ScopeResponse[]? granted;
            try
            {
                granted = await response.Content.ReadFromJsonAsync<ScopeResponse[]?>(cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                // 200 com corpo que não é a lista esperada = o provedor mudou de contrato. Tratar
                // como retrato vazio negaria TODO mundo em silêncio; indisponibilidade ao menos
                // grita 503 e deixa o fail-static segurar quem já estava usando.
                return RptFetchResult.Unavailable();
            }

            return RptFetchResult.Resolved(RptSnapshot.From(
                (granted ?? []).Select(entry => (entry.Rsname, (IReadOnlyCollection<string>)(entry.Scopes ?? [])))));
        }
    }

    private sealed record ScopeResponse(string Rsid, string Rsname, List<string>? Scopes);
}
