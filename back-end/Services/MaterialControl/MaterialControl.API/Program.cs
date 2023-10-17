using Commom.Auth.Authentication;
using Commom.Auth.Authorization;
using Commom.API.Filters;
using EntityFramework.Exceptions.PostgreSQL;
using MaterialControl.Infra.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MaterialControl.API.Configurations;
using Commom.MessageBroker;

//MaterialControl.API

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MaterialControlContext>(options =>
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

// messaging
builder.Services.AddMessageBrokerConfig(builder.Configuration, "material_control_id");


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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

        var context = services.GetRequiredService<MaterialControlContext>();
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