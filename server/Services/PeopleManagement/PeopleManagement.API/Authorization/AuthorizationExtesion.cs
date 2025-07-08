using Microsoft.AspNetCore.Authorization;
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
            services.AddSingleton<IAuthorizationHandler, ProtectedResourceRequirementHandler>();
            services.AddSingleton<IAuthorizationHandler, RouteAccessRequirementHandler>();

            services.AddHttpClient<IAuthorizationServerClient, AuthorizationServerClient>(httpClient =>
            {
                httpClient.BaseAddress = new Uri(authorizationOptions.KeycloakUrlRealm);
            }).AddHeaderPropagation();

            return services;
        }
    }
}

