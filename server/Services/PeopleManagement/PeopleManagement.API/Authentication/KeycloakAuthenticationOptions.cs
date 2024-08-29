using System.Diagnostics;
using System.Text.Json.Serialization;

namespace PeopleManagement.API.Authentication
{
    public class KeycloakAuthenticationOptions
    {
        private string authServerUrl = null!;

        private TimeSpan? tokenClockSkew;

        public const string Section = "Keycloak";

        public string Realm { get; set; } = default!;

        public string AuthServerUrl
        {
            get => this.authServerUrl;
            set => this.authServerUrl = NormalizeUrl(value);
        }

        public string SslRequired { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;

        public bool? VerifyTokenAudience { get; set; }

        public KeycloakClientInstallationCredentials Credentials { get; set; } = new();


        public string? Audience { get; set; }

        public string RoleClaimType { get; set; } = "role";

        public string NameClaimType { get; set; } = "preferred_username";

        public string OpenIdConnectUrl => $"{this.KeycloakUrlRealm}{".well-known/openid-configuration"}";
        public bool DisableRolesAccessTokenMapping { get; set; }


        public TimeSpan TokenClockSkew
        {
            get => this.tokenClockSkew ?? TimeSpan.Zero;
            set => this.tokenClockSkew = value;
        }

        

        public string KeycloakUrlRealm
        {
            get
            {
                return $"{this.AuthServerUrl}realms/{this.Realm}/";
            }
        }

        /// <summary>
        /// Token endpoint URL including Realm
        /// </summary>
        public string KeycloakTokenEndpoint
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.KeycloakUrlRealm))
                {
                    return default!;
                }

                return $"{this.KeycloakUrlRealm}{"protocol/openid-connect/token"}";
            }
        }

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
    /// Keycloak client credentials
    /// </summary>
    public class KeycloakClientInstallationCredentials
    {
        /// <summary>
        /// Secret
        /// </summary>
        public string Secret { get; set; } = string.Empty;
    }

}
