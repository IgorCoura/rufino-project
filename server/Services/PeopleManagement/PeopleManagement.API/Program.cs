using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.API.DependencyInjection;
using PeopleManagement.API.Filters;
using PeopleManagement.Application.Commands;
using PeopleManagement.Infra.Context;
using PeopleManagement.Services.DomainEventHandlers;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
//Config DataBase

var a = builder.Configuration.GetConnectionString("Postgresql");

builder.Services.AddDbContext<PeopleManagementContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgresql"),
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        })
        .UseExceptionProcessor();
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CommandAssembly).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(DomainEventHandlerAssembly).Assembly);
});

// Add services to the container.
builder.Services.AddInfraDependencies(builder.Configuration);
builder.Services.AddServicesDependencies(builder.Configuration);
builder.Services.AddApplicationDependencies(builder.Configuration);

builder.Services.AddControllers(opts =>
    {
        opts.Filters.Add(new ApplicationExceptionFilter());
    }
).AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (env != null && env.Equals("Development"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

var context = services.GetRequiredService<PeopleManagementContext>();
if (context.Database.GetPendingMigrations().Any())
{
    context.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }