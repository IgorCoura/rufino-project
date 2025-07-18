using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PeopleManagement.API.Authentication
{
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
                    OnAuthenticationFailed = context =>
                    {
                        // Log the exception details for debugging
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation(context.Exception, "Authentication failed. Exception: {ExceptionMessage}", context.Exception.Message);

                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync("{\"error\": \"Authentication failed\"}");
                    },
                    OnChallenge = context =>
                    {
                        // Log the challenge details for debugging
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Authentication challenge triggered. Scheme: {Scheme}, Error: {Error}, ErrorDescription: {ErrorDescription}",
                            context.Scheme.Name, context.Error, context.ErrorDescription);

                        return context.Response.WriteAsync("{\"error\": \"Unauthorized access\"}");
                    },
                    OnMessageReceived = context =>
                    {
                        // Log the received message for debugging
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Message received. Token: {Token}", context.Token);
                       
                        return Task.CompletedTask;
                    }
                };

            });

            return services;
        }
    }

  
}
