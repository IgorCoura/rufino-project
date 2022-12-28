using Commom.API.Authentication;
using Commom.API.Filters;
using Commom.Domain.PasswordHasher;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Service;
using Identity.API.Infrastructure.Context;
using Identity.API.Infrastructure.Repository;
using Microsoft.AspNetCore.Mvc;
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
builder.Services.AddScoped<IAuthService, AuthService>();

//Config Repositories 

builder.Services.AddScoped<IUserRefreshTokenRepository, UserRefreshTokenRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();


//Base
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddControllers(
    opts =>
    {
        opts.Filters.Add(new ApplicationExceptionFilter());
    }
);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<ApplicationContext>();
    if(context.Database.GetPendingMigrations().Any())
        context.Database.Migrate();
}

app.UseJwksDiscovery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
