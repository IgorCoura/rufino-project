using Microsoft.EntityFrameworkCore;
using TenantManagement.API.Authentication;
using TenantManagement.API.Authorization;
using TenantManagement.API.Extension;
using TenantManagement.API.Filters;
using TenantManagement.Application;
using TenantManagement.Infra;
using TenantManagement.Infra.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<DomainExceptionFilter>();
});

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddInfraDependencies(builder.Configuration);
builder.Services.AddCorsForFront(builder.Configuration, builder.Environment);

// Autenticação e autorização iguais às do PeopleManagement: JWT do Keycloak + policy montada
// em tempo de execução a partir de [ProtectedResource]. Os papéis vivem no realm, não aqui.
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();

var app = builder.Build();

// Migrações, não EnsureCreated: este decide por "o banco tem alguma tabela?", não por "o schema
// bate com o modelo?" — um Aggregate novo nunca ganharia tabela num banco já existente e a
// falha só apareceria na primeira consulta, como 42P01. Aconteceu no BillPayment em 2026-08-11.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "TenantManagement API");
    });

    app.MapGet("/", () => Results.LocalRedirect("~/swagger")).ExcludeFromDescription();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

public partial class Program;
