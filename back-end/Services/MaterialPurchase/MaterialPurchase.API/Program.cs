using Commom.API.Authentication;
using MaterialPurchase.API.Configurations;
using MaterialPurchase.Infra.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;
using Commom.API.AuthorizationIds;
using Commom.API.Filters;
using EntityFramework.Exceptions.PostgreSQL;

// MATERIA PURCHASE API

var builder = WebApplication.CreateBuilder(args);

//Config DataBase

builder.Services.AddDbContext<MaterialPurchaseContext>(options =>
{
    options.UseNpgsql(builder.Configuration["ConnectionString"],
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        })
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging()
        .UseExceptionProcessor();
});

// Add services to the container.

builder.Services.AddAuthenticationJwtBearer(builder.Configuration);
builder.Services.AddFunctionIdAuthorization<MaterialPurchaseContext>(builder.Configuration);
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


if (env != null && !env.Equals("Testing")) {

    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<MaterialPurchaseContext>();
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