namespace BillPayment.API.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class KeyclockExtesion
{
    public static JwtBearerOptions SetKeycloakOption(this JwtBearerOptions options, KeycloakAuthenticationOptions keycloakAuthenticationOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keycloakAuthenticationOptions);

        options.MetadataAddress = keycloakAuthenticationOptions.OpenIdConnectUrl;

        options.RequireHttpsMetadata = !string.IsNullOrWhiteSpace(keycloakAuthenticationOptions.SslRequired)
            && keycloakAuthenticationOptions.SslRequired.Equals("external", StringComparison.OrdinalIgnoreCase);

        options.Audience = keycloakAuthenticationOptions.Audience ?? keycloakAuthenticationOptions.Resource;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = keycloakAuthenticationOptions.VerifyTokenAudience ?? true,
            ValidateLifetime = true,
            ClockSkew = keycloakAuthenticationOptions.TokenClockSkew,
            NameClaimType = keycloakAuthenticationOptions.NameClaimType,
            RoleClaimType = keycloakAuthenticationOptions.RoleClaimType,
        };

        options.SaveToken = true;

        return options;
    }
}
