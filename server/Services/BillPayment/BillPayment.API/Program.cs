using BillPayment.API.BackgroundServices;
using BillPayment.API.Extension;
using BillPayment.API.Filters;
using BillPayment.Application;
using BillPayment.Infra;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<DomainExceptionFilter>();
});

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddInfraDependencies(builder.Configuration);
builder.Services.AddCorsForFront(builder.Configuration, builder.Environment);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();

// Agendador de captura. Desligado por padrão e registrado só quando Capture:Enabled=true — sem
// adapter de provedor ele só produziria falhas registradas em toda fonte, e a suíte de
// integração dirige a sincronização de forma determinística pelo endpoint manual.
builder.Services.Configure<CaptureSyncOptions>(builder.Configuration.GetSection(CaptureSyncOptions.SectionName));

if (builder.Configuration.GetValue<bool>($"{CaptureSyncOptions.SectionName}:Enabled"))
{
    builder.Services.AddHostedService<CaptureSyncBackgroundService>();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillPaymentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BillPayment API");
    });

    app.MapGet("/", () => Results.LocalRedirect("~/swagger")).ExcludeFromDescription();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

public partial class Program;
