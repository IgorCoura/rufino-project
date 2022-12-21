using Commom.API.Config;
using Commom.Domain.PasswordHasher;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var path = builder.Configuration["Jwt:Certs:Path"];

var dir = new DirectoryInfo(path.IsNullOrEmpty() ? Path.Combine(builder.Environment.ContentRootPath, "Certs") : path);

if (!dir.Exists)
    dir.Create();

builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
builder.Services.AddJwksManager().PersistKeysInMemory().PersistKeysToFileSystem(dir).UseJwtValidation();
builder.Services.AddAuthenticationJwtBearer(builder.Configuration);
builder.Services.AddPasswordHasher(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
