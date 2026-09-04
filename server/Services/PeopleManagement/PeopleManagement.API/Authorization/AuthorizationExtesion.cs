using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Infra.Services;

namespace PeopleManagement.API.Authorization
{
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
            // O cache do retrato de permissoes (plano de 2026-09-04). MemoryCache PROPRIO, com teto
            // de entradas: o cache compartilhado da aplicacao nao tem SizeLimit, e misturar
            // permissoes com o resto faria uma coisa despejar a outra.
            services.AddSingleton(_ => new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = authorizationOptions.RptCacheSizeLimit,
            }));
            services.AddSingleton<IRptCache>(provider => new RptCache(
                provider.GetRequiredService<IHttpContextAccessor>(),
                provider.GetRequiredService<IAuthorizationServerClient>(),
                provider.GetRequiredService<MemoryCache>(),
                authorizationOptions,
                provider.GetRequiredService<ILogger<RptCache>>()));

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
}

