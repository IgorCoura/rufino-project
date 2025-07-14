using EntityFramework.Exceptions.PostgreSQL;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.API.Authentication;
using PeopleManagement.API.Authorization;
using PeopleManagement.API.DependencyInjection;
using PeopleManagement.API.Filters;
using PeopleManagement.Application.Commands;
using PeopleManagement.Infra.Context;
using PeopleManagement.Infra.DataForTests;
using PeopleManagement.Services.DomainEventHandlers;
using PeopleManagement.Services.HangfireJobRegistrar;
using System.Diagnostics;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    serverOptions.Limits.MaxConcurrentConnections = 100;
    serverOptions.Limits.MaxConcurrentUpgradedConnections = 100;

});

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

//Config DataBase
var a = builder.Configuration.GetConnectionString("Postgresql");

builder.Services.AddDbContextFactory<PeopleManagementContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgresql"),
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        })
        .UseExceptionProcessor();
});

//Config Keycloak
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization(builder.Configuration);

// Add Hangfire services and configure PostgreSQL storage  
builder.Services.AddHangfire(configuration =>
   configuration.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("HangfireConnection"));
                }));

builder.Services.AddHangfireServer(); // Starts the Hangfire worker


builder.Services.AddCors(options => 
{ options.AddPolicy("CorsPolicy", builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()); 
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CommandAssembly>();
    cfg.RegisterServicesFromAssemblyContaining<DomainEventHandlerAssembly>();
});

builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddInfraDependencies(builder.Configuration);
builder.Services.AddServicesDependencies(builder.Configuration);
builder.Services.AddApplicationDependencies(builder.Configuration);



builder.Services.AddControllers(opts =>
{
    opts.Filters.Add<ApplicationExceptionFilter>();
}).AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Register ApplicationExceptionFilter with DI
builder.Services.AddScoped<ApplicationExceptionFilter>();


var app = builder.Build();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

var context = services.GetRequiredService<PeopleManagementContext>();
if (context.Database.GetPendingMigrations().Any())
{
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (env != null && env.Equals("Development"))
{
    
    var log = services.GetRequiredService<ILogger<Program>>();
    await PopulateDb.Populate(context, log);

    app.UseHangfireDashboard(options: new DashboardOptions
    {
        Authorization = new[] { new HangFireAuthorizationFilter() }
    });
}




app.UseSwagger();
app.UseSwaggerUI();



var jobScheduler = scope.ServiceProvider.GetRequiredService<HangfireJobRegister>();
jobScheduler.RegisterRecurringJobs();


app.UseHttpsRedirection();

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();




app.Run();


public partial class Program { }