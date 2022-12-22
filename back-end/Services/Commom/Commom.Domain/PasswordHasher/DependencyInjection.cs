using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commom.Domain.PasswordHasher
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPasswordHasher(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<PasswordHasherOptions>(config.GetSection(PasswordHasherOptions.PasswordHash));       
            services.AddScoped<IPasswordHasherService, PasswordHasherService>();

            return services;
        }
    }
}
