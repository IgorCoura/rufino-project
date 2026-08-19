namespace BillPayment.API.Authorization;

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

    public string KeycloakUrlRealm => $"{this.AuthServerUrl}realms/{this.Realm}/";

    public string TokenEndpointPath { get; set; } = "protocol/openid-connect/token";

    public string SourceAuthenticationScheme { get; set; } = JwtBearerDefaults.AuthenticationScheme;

    public string SourceTokenName { get; set; } = "Bearer";

    public string GrantType { get; set; } = "urn:ietf:params:oauth:grant-type:uma-ticket";

    public string Resource { get; set; } = string.Empty;

    public static bool DisableHeaderPropagation { get; set; }

    public bool UseProtectedResourcePolicyProvider { get; set; }

    public ScopesValidationMode ScopesValidationMode { get; set; } = ScopesValidationMode.AllOf;

    /// <summary>
    /// Nome do claim que lista os tenants da pessoa. É <c>tenants</c>, e não <c>tenant_ids</c>,
    /// porque o handler compara por <c>Contains</c>: com o nome alternativo,
    /// <c>"tenant_ids".Contains("tenants")</c> é falso e o guard reprovaria todo mundo. Quem
    /// emite o claim é o BC TenantManagement — simetria exata com o <c>companies</c> do
    /// PeopleManagement.
    /// </summary>
    public string RouteClaimTypeRequirement { get; set; } = "tenants";

    /// <summary>
    /// Nome do parâmetro de rota protegido. Toda rota de tenant deste BC é
    /// <c>api/v1/{tenantId}/...</c>; batizar o parâmetro de outra coisa faz o guard achar
    /// <c>null</c> e CONCEDER em silêncio — é o que o teste de erosão da suíte impede.
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

/// <summary>Como validar múltiplos escopos numa mesma permissão.</summary>
public enum ScopesValidationMode
{
    /// <summary>Todos os escopos precisam valer.</summary>
    AllOf,

    /// <summary>Basta um dos escopos valer.</summary>
    AnyOf,
}
