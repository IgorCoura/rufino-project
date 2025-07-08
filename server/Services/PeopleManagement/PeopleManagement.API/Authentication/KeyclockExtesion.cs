using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace PeopleManagement.API.Authentication
{
    public static class KeyclockExtesion
    {
        public static JwtBearerOptions SetKeycloakOption(this JwtBearerOptions options, KeycloakAuthenticationOptions keycloakAuthenticationOptions)
        {
            options.MetadataAddress = keycloakAuthenticationOptions.OpenIdConnectUrl;

            options.RequireHttpsMetadata = string.IsNullOrWhiteSpace(keycloakAuthenticationOptions.SslRequired) == false 
                && keycloakAuthenticationOptions.SslRequired.Equals("external", StringComparison.OrdinalIgnoreCase); 

            options.Audience = keycloakAuthenticationOptions.Audience ?? keycloakAuthenticationOptions.Resource;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = keycloakAuthenticationOptions.VerifyTokenAudience ?? true,
                ValidateLifetime = true,
                ClockSkew = keycloakAuthenticationOptions.TokenClockSkew,
                NameClaimType = keycloakAuthenticationOptions.NameClaimType,
                RoleClaimType = keycloakAuthenticationOptions.RoleClaimType
            };

            options.SaveToken = true;

            return options;
        }
    }
}
