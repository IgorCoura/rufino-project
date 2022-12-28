using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Security.Jwt.Core;
using Security.Jwt.Core.Interfaces;
using Security.Jwt.Store.FileSystem;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder extension methods for registering crypto services
/// </summary>
public static class FileSystemStoreExtensions
{
    /// <summary>
    /// Sets the signing credential.
    /// </summary>
    /// <returns></returns>
    public static IJwksBuilder PersistKeysToFileSystem(this IJwksBuilder builder, DirectoryInfo directory)
    {
        builder.Services.AddScoped<IJsonWebKeyStore, FileSystemStore>(provider => new FileSystemStore(directory, provider.GetRequiredService<IOptions<JwtOptions>>(), provider.GetRequiredService<IMemoryCache>()));

        return builder;
    }
}