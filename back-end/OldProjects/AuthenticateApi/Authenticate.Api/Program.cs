using Authenticate.Api.Interfaces;
using Authenticate.Api.Options;
using Authenticate.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


//AddApiVersioning

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


//Api Options

builder.Services.Configure<TokenGeneratorOptions>(builder.Configuration.GetSection("Jwt"));

//Services 

builder.Services.AddHttpClient("AuthApi", c => c.BaseAddress = new Uri("https://localhost:8011"));
builder.Services.AddSingleton<IAuthService, AuthService>();

//Autentication

builder.Services.AddIdentityServer()
        .AddDeveloperSigningCredential()        //This is for dev only scenarios when you don’t have a certificate to use.
        .AddInMemoryApiScopes(Config.ApiScopes)
        .AddInMemoryClients(Config.Clients);


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

//app.UseHttpsRedirection();

app.UseIdentityServer();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
