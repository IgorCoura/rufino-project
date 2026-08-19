namespace BillPayment.API.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

/// <summary>
/// Autorização idêntica à do PeopleManagement e à do TenantManagement (ADR-004 daquele BC):
/// <c>[ProtectedResource(recurso, escopo)]</c> monta a policy em tempo de execução, o handler
/// pede um RPT ao Keycloak por ticket UMA propagando o token do chamador, e quem decide quais
/// papéis alcançam cada recurso é a configuração do realm — nunca o código.
/// </summary>
/// <remarks>
/// Toda policy carrega DOIS requirements: o <c>RouteAccessRequirement</c>, que confere o
/// <c>{tenantId}</c> da rota contra o claim <c>tenants</c> (anti-IDOR), e o
/// <c>ProtectedResourceRequirement</c>, que pergunta ao servidor de autorização se aquela pessoa
/// tem o escopo. Um sem o outro deixa metade da porta aberta: o primeiro sozinho não distingue
/// quem lê de quem aprova; o segundo sozinho deixa quem tem <c>bill:approve</c> aprovar boleto
/// de qualquer tenant.
/// </remarks>
public static class AuthorizationExtesion
{
    public static IServiceCollection AddKeycloakAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

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
            }));

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
