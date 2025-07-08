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

            });

            return services;
        }
    }

  
}
