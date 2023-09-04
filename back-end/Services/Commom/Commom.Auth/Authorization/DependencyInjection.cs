using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commom.Auth.Authorization
{ 
    public static class DependencyInjection
    {
        public static IServiceCollection AddBaseAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AuthorizationIdOptions>(configuration.GetSection(AuthorizationIdOptions.Section));
            services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationIdPolicyProvider>();

            return services;
        }
    }
}
