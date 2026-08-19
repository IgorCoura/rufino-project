namespace BillPayment.API.Extension;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Declara o esquema Bearer no documento OpenAPI.
/// </summary>
/// <remarks>
/// Sem isto o botão <em>Authorize</em> não existe no Swagger UI e toda tentativa pela tela volta
/// 401 — a UI continuaria montando as requisições sem header nenhum, e o desenvolvedor concluiria
/// que a API está quebrada. É a única razão de o esquema estar no documento: ele não protege
/// nada, quem protege é a policy.
/// </remarks>
public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiWithBearer(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);

            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Access token do Keycloak (realm rufino). O tenant da rota precisa estar no claim 'tenants'.",
            };

            return Task.CompletedTask;
        }));

        return services;
    }
}
