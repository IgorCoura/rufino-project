namespace EconomicCore.API.Extension;

public static class CorsExtensions
{
    public static IServiceCollection AddCorsForFront(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment environment)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(opts =>
        {
            opts.AddDefaultPolicy(policy =>
            {
                if (environment.IsDevelopment() && origins.Length == 0)
                {
                    policy
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    policy
                        .WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithExposedHeaders("X-Correlation-Id");
                }
            });
        });

        return services;
    }
}
