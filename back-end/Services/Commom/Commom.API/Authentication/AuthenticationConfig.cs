﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Security.JwtExtensions;

namespace Commom.API.Authentication
{
    public static class AuthenticationConfig
    {
        public static IServiceCollection AddAuthenticationJwtBearer(this IServiceCollection services, IConfiguration config)
        {

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.SetJwksOptions(new JwkOptions(config["Authentication:JwksUri"], audience: config["Authentication:Audience"]));
                x.TokenValidationParameters.ValidateLifetime = true;
                x.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("IS-TOKEN-EXPIRED", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            }); 

            return services;
        }
    }
}
