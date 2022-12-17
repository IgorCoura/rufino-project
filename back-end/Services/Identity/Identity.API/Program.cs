using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Security.JwtExtensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var dir = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "TempKeys"));

if (!dir.Exists)
    dir.Create();

builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
builder.Services.AddJwksManager().PersistKeysInMemory().PersistKeysToFileSystem(dir).UseJwtValidation();
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.IncludeErrorDetails = true;
    x.SetJwksOptions(new JwkOptions("http://identity.api:80/jwks"));
});


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
