using Commom.API.Filters;
using Microsoft.EntityFrameworkCore;
using EntityFramework.Exceptions.PostgreSQL;
using CustSuprManager.Infra.Context;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using CustSuprManager.API.Configurations;
using Commom.Auth.Authentication;
using Commom.Auth.Authorization;

//CustSuprManager.API

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CustSuprManagerContext>(options =>
{
    options.UseNpgsql(builder.Configuration["Database:ConnectionString"],
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        })
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
        .EnableDetailedErrors() //TODO: Remover na produção
        .EnableSensitiveDataLogging()  //TODO: Remover na produção
        .UseExceptionProcessor();

});


// Add services to the container.

builder.Services.AddAuthenticationJwtBearer(builder.Configuration);
builder.Services.AddBaseAuthorization(builder.Configuration);
builder.Services.AddDependencies(builder.Configuration);

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
).AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

// Configure the HTTP request pipeline.
if (env != null && env.Equals("Development"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (env != null && !env.Equals("Testing"))
{

    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<CustSuprManagerContext>();
        if (context.Database.GetPendingMigrations().Any())
            context.Database.Migrate();
    }
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }