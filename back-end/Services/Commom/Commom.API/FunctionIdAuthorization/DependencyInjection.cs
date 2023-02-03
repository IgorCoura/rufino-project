using Commom.Infra.Base;
using Commom.Infra.Interface;
using Commom.Infra.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.API.FunctionIdAuthorization
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddFunctionIdAuthorization<context>(this IServiceCollection services, IConfiguration configuration) where context : BaseContext
        {
            services.Configure<FunctionIdAuthorizationOptions>(configuration.GetSection(FunctionIdAuthorizationOptions.Section));

            services.AddSingleton<IAuthorizationPolicyProvider, FunctionIdPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, FunctionIdHandler>();
            services.AddScoped<IRoleRepository, RoleRepository<context>>();

            return services;
        }
    }
}
