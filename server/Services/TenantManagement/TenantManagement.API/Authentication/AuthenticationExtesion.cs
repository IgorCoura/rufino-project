namespace TenantManagement.API.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;

public static class AuthenticationExtesion
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string authenticationScheme = JwtBearerDefaults.AuthenticationScheme,
        string defaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme
        )
    {
        var builder = services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = authenticationScheme;
                x.DefaultChallengeScheme = defaultChallengeScheme;
            });

        var keycloakAuthenticationOptions = new KeycloakAuthenticationOptions();

        configuration.GetSection(KeycloakAuthenticationOptions.Section).Bind(keycloakAuthenticationOptions);

        builder.AddJwtBearer(options =>
        {
            options.SetKeycloakOption(keycloakAuthenticationOptions);
            options.Events = new JwtBearerEvents
            {
                // Só registra. Quem escreve a resposta é o OnChallenge, que roda logo em
                // seguida — escrever aqui também produziria dois corpos JSON concatenados.
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation(context.Exception, "Authentication failed. Exception: {ExceptionMessage}", context.Exception.Message);

                    return Task.CompletedTask;
                },

                // A ORDEM AQUI É O QUE DEFINE O STATUS DA RESPOSTA, e errá-la é silencioso.
                // Escrever o corpo sem HandleResponse() e sem StatusCode faz a resposta ser
                // commitada com o 200 padrão: uma requisição SEM TOKEN devolvia
                // `200 {"error": "Unauthorized access"}`, e qualquer cliente que olhe o status
                // — o nosso olha — trataria a negativa como sucesso. Medido em contêiner.
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("Authentication challenge triggered. Scheme: {Scheme}, Error: {Error}, ErrorDescription: {ErrorDescription}",
                            context.Scheme.Name, context.Error, context.ErrorDescription);

                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"error\": \"Unauthorized access\"}");
                },
            };

        });

        return services;
    }
}


