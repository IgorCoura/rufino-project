using EconomicCore.API.Filters;
using EconomicCore.Application;
using EconomicCore.Infra;
using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<DomainExceptionFilter>();
});

builder.Services.AddApplicationDependencies();
builder.Services.AddInfraDependencies(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EconomicCoreDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "EconomicCore API");
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
