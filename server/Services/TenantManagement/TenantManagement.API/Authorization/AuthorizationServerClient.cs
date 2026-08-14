namespace TenantManagement.API.Authorization;

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
            // Keycloak answers 401 when the propagated access token itself is
            // rejected (expired, revoked, not-before) — that is an authentication
            // problem, not a missing permission, and must surface as 401 upstream.
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

            var scopesGranted = await this.ValidateScopesAsync(
                    permission,
                    response,
                    cancellationToken
                );

            return scopesGranted ? ResourceAccessResult.Granted : ResourceAccessResult.Denied;
        }
    }

    private Dictionary<string, string> GetContentRequest(string permission)
    {
        var audience = _authorizationOptions.Resource;
        var responseMode =  AuthorizationOptions.ResponseMode(permission.Contains(',', StringComparison.Ordinal));

        return new Dictionary<string, string>
        {
            { "grant_type", _authorizationOptions.GrantType},
            { "response_mode", responseMode },
            { "audience", audience },
            { "permission", permission }
        };
    }

    private async Task<bool> ValidateScopesAsync(string permission,
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var resource = permission.Split("#")[0];
        var scope = permission.Split("#")[1];
        var scopes = scope.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (scopes is { Count: <= 1 })
            return true;

        var scopeResponse = await response.Content.ReadFromJsonAsync<ScopeResponse[]?>(
            cancellationToken: cancellationToken
        );

        return this.ValidateScopes(resource, scopes, scopeResponse);
    }

    private bool ValidateScopes(string resource, List<string> scopesToValidate, ScopeResponse[]? scopeResponse)
    {
        scopeResponse ??= [];

        var resourceToValidate = Array.Find(
            scopeResponse,
            r => string.Equals(r.Rsname, resource, StringComparison.Ordinal)
        );

        // Resource absent from the RPT means the server granted no scopes on it:
        // a plain permission denial, never an unhandled exception (which would
        // bubble out of the authorization middleware as a 500).
        if (resourceToValidate is null)
        {
            return false;
        }

        if (_authorizationOptions.ScopesValidationMode == ScopesValidationMode.AllOf)
        {
            var resourceScopes = resourceToValidate.Scopes;
            var allScopesPresent = scopesToValidate.TrueForAll(s => resourceScopes.Contains(s));

            return allScopesPresent;
        }

        else if (_authorizationOptions.ScopesValidationMode == ScopesValidationMode.AnyOf)
        {
            var resourceScopes = resourceToValidate.Scopes;
            var anyScopePresent = scopesToValidate.Exists(s => resourceScopes.Contains(s));

            return anyScopePresent;
        }

        return true;
    }


    private sealed record ScopeResponse(string Rsid, string Rsname, List<string> Scopes);
}
