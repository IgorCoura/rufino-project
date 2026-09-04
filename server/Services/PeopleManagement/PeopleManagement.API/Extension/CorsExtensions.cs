namespace PeopleManagement.API.Extension
{
    /// <summary>
    /// CORS por allowlist. Substituiu o <c>AllowAnyOrigin()</c> que valia desde o início do BC.
    /// </summary>
    /// <remarks>
    /// Em Development com a lista vazia, qualquer origem é aceita — é o que mantém o Flutter web
    /// em porta aleatória funcionando sem ninguém editar configuração. Fora de Development a lista
    /// manda, e vazia significa nenhuma origem de navegador: uma API sem front declarado não
    /// precisa de CORS, e o silêncio aqui é mais seguro que um curinga esquecido.
    /// </remarks>
    public static class CorsExtensions
    {
        public static IServiceCollection AddCorsForFront(
            this IServiceCollection services,
            IConfiguration config,
            IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(environment);

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
}
