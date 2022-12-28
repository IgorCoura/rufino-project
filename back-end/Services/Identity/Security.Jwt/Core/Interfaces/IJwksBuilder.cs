using Microsoft.Extensions.DependencyInjection;

namespace Security.Jwt.Core.Interfaces;

public interface IJwksBuilder
{
    IServiceCollection Services { get; }
}