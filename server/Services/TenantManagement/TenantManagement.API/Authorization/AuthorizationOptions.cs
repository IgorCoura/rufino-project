namespace TenantManagement.API.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;

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
    /// <summary>
    /// Nome do claim que lista os tenants da pessoa. É <c>tenants</c>, e não
    /// <c>tenant_ids</c>, porque o handler compara por <c>Contains</c>: com o nome
    /// alternativo, <c>"tenant_ids".Contains("tenants")</c> é falso e o guard reprovaria
    /// todo mundo. Simetria exata com o <c>companies</c> do PeopleManagement.
    /// </summary>
    public string RouteClaimTypeRequirement { get; set; } = "tenants";

    /// <summary>
    /// Nome do parâmetro de rota protegido. Só as rotas do próprio tenant o usam; as de
    /// back-office chamam o parâmetro de <c>id</c> e não passam por este guard.
    /// </summary>
    public string RouteNameRequirement { get; set; } = "tenantId";
    public static string ResponseMode(bool isDecisionMode) => isDecisionMode ? "permissions" : "decision";


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
