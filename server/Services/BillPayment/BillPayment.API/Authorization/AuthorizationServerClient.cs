namespace BillPayment.API.Authorization;

using System.Net;

public class AuthorizationServerClient(HttpClient httpClient, AuthorizationOptions authorizationOptions) : IAuthorizationServerClient
{
    private readonly HttpClient _httpClient = httpClient;

    private readonly AuthorizationOptions _authorizationOptions = authorizationOptions;

    public async Task<ResourceAccessResult> VerifyAccessToResouce(string permission, CancellationToken cancellationToken = default)
    {
        using var content = new FormUrlEncodedContent(this.GetContentRequest(permission));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(_authorizationOptions.TokenEndpointPath, content, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ResourceAccessResult.ServerUnavailable;
        }

        using (response)
        {
            // O Keycloak responde 401 quando o token propagado é que foi recusado (expirado,
            // revogado, not-before) — isso é problema de AUTENTICAÇÃO, não permissão faltando,
            // e precisa chegar ao cliente como 401 para ele renovar o token.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return ResourceAccessResult.InvalidToken;
            }

            if ((int)response.StatusCode >= 500)
            {
                return ResourceAccessResult.ServerUnavailable;
            }

            if (!response.IsSuccessStatusCode)
            {
                return ResourceAccessResult.Denied;
            }

            var scopesGranted = await this.ValidateScopesAsync(permission, response, cancellationToken);

            return scopesGranted ? ResourceAccessResult.Granted : ResourceAccessResult.Denied;
        }
    }

    private Dictionary<string, string> GetContentRequest(string permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        var audience = _authorizationOptions.Resource;
        var responseMode = AuthorizationOptions.ResponseMode(permission.Contains(',', StringComparison.Ordinal));

        return new Dictionary<string, string>
        {
            { "grant_type", _authorizationOptions.GrantType },
            { "response_mode", responseMode },
            { "audience", audience },
            { "permission", permission },
        };
    }

    private async Task<bool> ValidateScopesAsync(string permission, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var resource = permission.Split("#")[0];
        var scope = permission.Split("#")[1];
        var scopes = scope.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (scopes is { Count: <= 1 })
            return true;

        var scopeResponse = await response.Content.ReadFromJsonAsync<ScopeResponse[]?>(cancellationToken: cancellationToken);

        return this.ValidateScopes(resource, scopes, scopeResponse);
    }

    private bool ValidateScopes(string resource, List<string> scopesToValidate, ScopeResponse[]? scopeResponse)
    {
        scopeResponse ??= [];

        var resourceToValidate = Array.Find(scopeResponse, r => string.Equals(r.Rsname, resource, StringComparison.Ordinal));

        // Recurso ausente do RPT significa que o servidor não concedeu escopo nenhum sobre ele:
        // é negativa de permissão, nunca exceção não tratada (que sairia do middleware como 500).
        if (resourceToValidate is null)
        {
            return false;
        }

        if (_authorizationOptions.ScopesValidationMode == ScopesValidationMode.AllOf)
        {
            var resourceScopes = resourceToValidate.Scopes;
            return scopesToValidate.TrueForAll(s => resourceScopes.Contains(s));
        }
        else if (_authorizationOptions.ScopesValidationMode == ScopesValidationMode.AnyOf)
        {
            var resourceScopes = resourceToValidate.Scopes;
            return scopesToValidate.Exists(s => resourceScopes.Contains(s));
        }

        return true;
    }

    private sealed record ScopeResponse(string Rsid, string Rsname, List<string> Scopes);
}
