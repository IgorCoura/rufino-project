using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PeopleManagement.API.Authorization
{
    public class AuthorizationOptions
    {

        public const string Section = "Keycloak";

        private string authServerUrl = null!;
        public string Realm { get; set; } = default!;
        public string AuthServerUrl
        {
            get => this.authServerUrl;
            set => this.authServerUrl = NormalizeUrl(value);
        }
        public string KeycloakUrlRealm
        {
            get
            {
                return $"{this.AuthServerUrl}realms/{this.Realm}/";
            }
        }
        public string TokenEndpointPath { get; set; } = "protocol/openid-connect/token";
        public string SourceAuthenticationScheme { get; set; } = JwtBearerDefaults.AuthenticationScheme;
        public string SourceTokenName { get; set; } = "Bearer";
        public string GrantType { get; set; } = "urn:ietf:params:oauth:grant-type:uma-ticket";
        
        public string Resource { get; set; } = string.Empty;

        public static bool DisableHeaderPropagation { get; set; }

        public bool UseProtectedResourcePolicyProvider { get; set; }

        public ScopesValidationMode ScopesValidationMode { get; set; } = ScopesValidationMode.AllOf;
        public string RouteClaimTypeRequirement { get; set; } = "companies";
        public string RouteNameRequirement { get; set; } = "company";
        public string ResponseMode(bool isDecisionMode) => isDecisionMode ? "permissions" : "decision";


        private static string NormalizeUrl(string url)
        {
            if (!url.EndsWith('/'))
            {
                url += "/";
            }

            return url;
        }
    }

    /// <summary>
    /// Specifies the validation mode for multiple scopes.
    /// </summary>
    public enum ScopesValidationMode
    {
        /// <summary>
        /// Specifies that all of the scopes must be valid.
        /// </summary>
        AllOf,

        /// <summary>
        /// Specifies that at least one of the scopes must be valid.
        /// </summary>
        AnyOf,
    }
}
