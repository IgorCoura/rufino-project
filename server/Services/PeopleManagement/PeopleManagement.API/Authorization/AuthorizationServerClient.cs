using System.Threading;

namespace PeopleManagement.API.Authorization
{
    public class AuthorizationServerClient(HttpClient httpClient, AuthorizationOptions authorizationOptions) : IAuthorizationServerClient
    {
        private readonly HttpClient _httpClient = httpClient;

        private readonly AuthorizationOptions _authorizationOptions = authorizationOptions;

        public async Task<bool> VerifyAccessToResouce(string permission, CancellationToken cancellationToken = default) 
        {

            using var content = new FormUrlEncodedContent(this.GetContentRequest(permission));
            var response = await _httpClient.PostAsync(_authorizationOptions.TokenEndpointPath, content, cancellationToken);

            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return await this.ValidateScopesAsync(
                    permission,
                    response,
                    cancellationToken
                );
        }

        private Dictionary<string, string> GetContentRequest(string permission)
        {
            var audience = _authorizationOptions.Resource;
            var responseMode =  _authorizationOptions.ResponseMode(permission.Contains(','));

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

            return this.ValidateScopesAsync(resource, scopes, scopeResponse);
        }

        private bool ValidateScopesAsync(string resource, List<string> scopesToValidate, ScopeResponse[]? scopeResponse)
        {
            scopeResponse ??= [];

            var resourceToValidate = Array.Find(
                scopeResponse,
                r => string.Equals(r.Rsname, resource, StringComparison.Ordinal)
            );

            if (resourceToValidate is null)
            {
                throw new Exception($"Unable to find a resource - {resource}");
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
}
