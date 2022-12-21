using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.PasswordHasher
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPasswordHasher(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<PasswordHasherOptions>(options => config.GetSection(PasswordHasherOptions.PasswordHash));       
            services.AddScoped<IPasswordHasherService, PasswordHasherService>();

            return services;
        }
    }
}
