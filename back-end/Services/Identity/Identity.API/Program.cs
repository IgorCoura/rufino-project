using Commom.API.Authentication;
using Commom.Domain.PasswordHasher;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Service;
using Identity.API.Infrastructure.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Config Security

var path = builder.Configuration["Jwt:Certs:Path"];

var dir = new DirectoryInfo(path.IsNullOrEmpty() ? Path.Combine(builder.Environment.ContentRootPath, "Certs") : path);

if (!dir.Exists)
    dir.Create();

builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
builder.Services.AddJwksManager().PersistKeysInMemory().PersistKeysToFileSystem(dir).UseJwtValidation();
builder.Services.AddAuthenticationJwtBearer(builder.Configuration);
builder.Services.AddPasswordHasher(builder.Configuration);

//Config DataBase

builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options.UseNpgsql(builder.Configuration["ConnectionString"],
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        });
});


//Config Services

builder.Services.AddScoped<ITokenService, TokenService>();

//Config Repositories 


//Base
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseJwksDiscovery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
