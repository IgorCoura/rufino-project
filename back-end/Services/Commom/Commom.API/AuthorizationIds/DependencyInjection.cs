using Commom.Infra.Base;
using Commom.Infra.Interface;
using Commom.Infra.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commom.API.AuthorizationIds
{ 
    public static class DependencyInjection
    {
        public static IServiceCollection AddFunctionIdAuthorization<context>(this IServiceCollection services, IConfiguration configuration) where context : BaseContext
        {
            services.Configure<AuthorizationIdOptions>(configuration.GetSection(AuthorizationIdOptions.Section));

            services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationIdPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, AuthorizationIdHandler>();
            services.AddScoped<IRoleRepository, RoleRepository<context>>();

            return services;
        }
    }
}
