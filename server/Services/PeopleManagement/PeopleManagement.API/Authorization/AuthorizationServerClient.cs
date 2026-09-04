using System.Net;
using System.Text.Json;

namespace PeopleManagement.API.Authorization
{
    /// <summary>
    /// Fala com o servidor de autorizacao por ticket UMA, propagando o token de quem chamou.
    /// </summary>
    /// <remarks>
    /// Desde 2026-09-04 tem UM metodo, e ele busca o retrato inteiro. Antes era um por permissao
    /// (<c>VerifyAccessToResouce</c>), o que fazia todo endpoint protegido ir ao Keycloak a cada
    /// requisicao. Quem resolve permissao agora e o <see cref="RptSnapshot"/>, em memoria; ver
    /// <see cref="RptCache"/>.
    /// </remarks>
    public class AuthorizationServerClient(HttpClient httpClient, AuthorizationOptions authorizationOptions) : IAuthorizationServerClient
    {
        private readonly HttpClient _httpClient = httpClient;

        private readonly AuthorizationOptions _authorizationOptions = authorizationOptions;

        public async Task<RptFetchResult> FetchAllPermissionsAsync(CancellationToken cancellationToken = default)
        {
            // SEM o parametro "permission": e o que faz o Keycloak avaliar todas as permissoes que
            // a pessoa alcanca neste resource server, em vez de responder sobre uma so. O
            // response_mode tem que ser "permissions" — "decision" devolveria um RPT sem a lista,
            // que e justamente o que precisamos guardar.
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
                // Sem este catch ele escaparia do handler de autorizacao e sairia como 500.
                return RptFetchResult.Unavailable();
            }

            using (response)
            {
                // O Keycloak responde 401 quando o token propagado e que foi recusado (expirado,
                // revogado, not-before) — isso e problema de AUTENTICACAO, nao permissao faltando,
                // e precisa chegar ao cliente como 401 para ele renovar o token.
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RptFetchResult.InvalidToken();

                if ((int)response.StatusCode >= 500)
                    return RptFetchResult.Unavailable();

                // 403 aqui e o servidor dizendo "esta pessoa nao alcanca nada" — retrato VAZIO, nao
                // falha. Trata-lo como indisponibilidade faria quem nao tem permissao receber 503,
                // e pior: com fail-static, herdar o retrato anterior de outra sessao.
                if (!response.IsSuccessStatusCode)
                    return RptFetchResult.Resolved(RptSnapshot.Empty);

                ScopeResponse[]? granted;
                try
                {
                    granted = await response.Content.ReadFromJsonAsync<ScopeResponse[]?>(cancellationToken: cancellationToken);
                }
                catch (JsonException)
                {
                    // 200 com corpo que nao e a lista esperada = o provedor mudou de contrato.
                    // Tratar como retrato vazio negaria TODO mundo em silencio; indisponibilidade
                    // ao menos grita 503 e deixa o fail-static segurar quem ja estava usando.
                    return RptFetchResult.Unavailable();
                }

                return RptFetchResult.Resolved(RptSnapshot.From(
                    (granted ?? []).Select(entry => (entry.Rsname, (IReadOnlyCollection<string>)(entry.Scopes ?? [])))));
            }
        }

        private sealed record ScopeResponse(string Rsid, string Rsname, List<string>? Scopes);
    }
}
