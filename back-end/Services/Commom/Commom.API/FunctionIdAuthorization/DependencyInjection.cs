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
        public static IServiceCollection AddFunctionIdAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FunctionIdAuthorizationOptions>(configuration.GetSection(FunctionIdAuthorizationOptions.Jwt));

            services.AddSingleton<IAuthorizationPolicyProvider, FunctionIdPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, FunctionIdHandler>();

            return services;
        }
    }
}
