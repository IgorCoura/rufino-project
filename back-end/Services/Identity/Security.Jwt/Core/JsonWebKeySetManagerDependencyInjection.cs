using Security.Jwt.Core;
using Security.Jwt.Core.DefaultStore;
using Security.Jwt.Core.Interfaces;
using Security.Jwt.Core.Jwt;

namespace Microsoft.Extensions.DependencyInjection;

public static class JsonWebKeySetManagerDependencyInjection
{
    /// <summary>
    /// Sets the signing credential.
    /// </summary>
    /// <returns></returns>
    public static IJwksBuilder AddJwksManager(this IServiceCollection services, Action<JwtOptions>? action = null)
    {
        if (action != null)
            services.Configure(action);

        services.AddDataProtection();
        services.AddScoped<IJwtService, JwtService>();
        
            
        return new JwksBuilder(services);
    }

    /// <summary>
    /// Sets the signing credential.
    /// </summary>
    /// <returns></returns>
    public static IJwksBuilder PersistKeysInMemory(this IJwksBuilder builder)
    {
        builder.Services.AddScoped<IJsonWebKeyStore, InMemoryStore>();

        return builder;
    }
}