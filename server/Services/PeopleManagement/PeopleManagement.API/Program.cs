using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.API.DependencyInjection;
using PeopleManagement.API.Filters;
using PeopleManagement.Application.Commands;
using PeopleManagement.Infra.Context;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//Config DataBase

builder.Services.AddDbContext<PeopleManagementContext>(options =>
{
    options.UseNpgsql(builder.Configuration["Database:ConnectionString"],
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        })
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
        .UseExceptionProcessor();
});

// Add services to the container.

builder.Services.AddInfraDependencies(builder.Configuration);
builder.Services.AddApplicationDependencies(builder.Configuration);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CommandAssembly).Assembly);
});

builder.Services.AddControllers(opts =>
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

    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<PeopleManagementContext>();
    if (context.Database.GetPendingMigrations().Any())
        context.Database.Migrate();
}


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
