namespace TenantManagement.API.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

/// <summary>
/// Autorização idêntica à do PeopleManagement: <c>[ProtectedResource(recurso, escopo)]</c>
/// monta a policy em tempo de execução, o handler pede um RPT ao Keycloak por ticket UMA
/// propagando o token do chamador, e quem decide quais papéis alcançam cada recurso é a
/// configuração do realm — nunca o código.
/// </summary>
/// <remarks>
/// O <c>RouteAccessRequirement</c> casa pelo <strong>nome do parâmetro de rota</strong>
/// (<c>tenantId</c>). Endpoints de back-office usam <c>{id}</c> de propósito: um operador
/// da plataforma não tem tenant no claim, e batizar o parâmetro de <c>tenantId</c> o
/// trancaria para fora com um 403 sem explicação.
/// </remarks>
public static class AuthorizationExtesion
{
    public static IServiceCollection AddKeycloakAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        var authorizationOptions = new AuthorizationOptions();
        configuration.GetSection(AuthorizationOptions.Section).Bind(authorizationOptions);
        services.AddSingleton(authorizationOptions);

        services.AddHttpContextAccessor();

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider>(x => new ProtectedResourcePolicyProvider(
            param =>
            {
                var policy = new AuthorizationPolicyBuilder(authorizationOptions.SourceAuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new RouteAccessRequirement(authorizationOptions.RouteNameRequirement, authorizationOptions.RouteClaimTypeRequirement));
                policy.AddRequirements(new ProtectedResourceRequirement(param));
                return policy;
            })
        );
        services.AddSingleton<IAuthorizationHandler, ProtectedResourceRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, RouteAccessRequirementHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationResultHandler>();

        services.AddHttpClient<IAuthorizationServerClient, AuthorizationServerClient>(httpClient =>
        {
            httpClient.BaseAddress = new Uri(authorizationOptions.KeycloakUrlRealm);
        }).AddHeaderPropagation();

        return services;
    }
}

