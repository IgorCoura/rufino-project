using EntityFramework.Exceptions.PostgreSQL;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.API.Authentication;
using PeopleManagement.API.Authorization;
using PeopleManagement.API.DependencyInjection;
using PeopleManagement.API.Extension;
using PeopleManagement.API.Filters;
using PeopleManagement.Application.Commands;
using PeopleManagement.Domain.Options;
using PeopleManagement.Infra.Context;
using PeopleManagement.Infra.DataForTests;
using PeopleManagement.Services.DomainEventHandlers;
using PeopleManagement.Services.HangfireJobRegistrar;
using PeopleManagement.Services.Services;
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
var messagingQueueOptions = builder.Configuration.GetSection(MessagingQueueOptions.SectionName).Get<MessagingQueueOptions>()
    ?? new MessagingQueueOptions();

//Config DataBase
builder.Services.AddDbContext<PeopleManagementContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PeopleManagement"),
        npgsqlOptionsAction: sqlOptions =>
        {
            // O schema é obrigatório aqui. A connection string traz SearchPath=people_management, e o
            // Migrate cria a tabela de histórico ANTES de aplicar qualquer migração — ou seja, antes do
            // EnsureSchema que criaria o schema. Sem o schema explícito o CREATE TABLE sai sem qualificação,
            // o Postgres procura no search_path vazio e morre em 3F000 "no schema has been selected to
            // create in" em todo banco virgem. O nome tem que continuar sendo o padrão do EF: os ambientes
            // já existentes gravaram o histórico com ele, e renomear faria o EF reaplicar tudo do zero.
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PeopleManagementContext.DEFAULT_SCHEMA);
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        })
        .UseExceptionProcessor();
}, ServiceLifetime.Scoped);

// Adicionar factory para as queries que precisam criar múltiplas instâncias
builder.Services.AddDbContextFactory<PeopleManagementContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PeopleManagement"),
        npgsqlOptionsAction: sqlOptions =>
        {
            // Tem que repetir o do registro acima: esta é a última registração de
            // DbContextOptions<PeopleManagementContext>, então é ela que o contexto resolvido pelo DI
            // recebe — inclusive o que roda o Migrate. Configurar só num dos dois deixa o histórico
            // de migração em lugar diferente do que o outro procura.
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PeopleManagementContext.DEFAULT_SCHEMA);
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        })
        .UseExceptionProcessor();
}, ServiceLifetime.Scoped); // Adicione este segundo parâmetro

//Config Keycloak
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization(builder.Configuration);

// Add Hangfire services and configure PostgreSQL storage  
builder.Services.AddHangfire(configuration =>
   configuration.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("BackgroundJobs"));
                }));

// Em testes de integração os workers do Hangfire ficam desligados (determinismo): os jobs são apenas
// agendados no storage, sem processamento — evita chamadas externas (ex.: refresh de webhook ZapSign)
// e crashes por acesso a serviços já descartados após o fim da suíte.
if (!string.Equals(builder.Environment.EnvironmentName, "IntegrationTest", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHangfireServer(options =>
    {
        options.ServerName = "default-worker";
        options.Queues = new[] { "default" };
    });

    builder.Services.AddHangfireServer(options =>
    {
        options.ServerName = "whatsapp-serial-worker";
        options.Queues = new[] { messagingQueueOptions.QueueName };
        options.WorkerCount = 1;
    });
}


builder.Services.AddCorsForFront(builder.Configuration, builder.Environment);
builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CommandAssembly>();
    cfg.RegisterServicesFromAssemblyContaining<DomainEventHandlerAssembly>();
});

builder.Services.AddHttpClient();

// Add memory cache for authorization token caching
builder.Services.AddMemoryCache();

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
// Migrate vem PRIMEIRO e incondicional: ele cria o database quando não existe (Postgres virgem)
// e é no-op quando tudo já foi aplicado. Qualquer comando cru antes dele — o CREATE SCHEMA
// abaixo, ou o GetPendingMigrations do if antigo — abre conexão direta no database e morre em
// 3D000 num volume novo, antes de o Migrate ter chance de criá-lo.
context.Database.Migrate();
//context.Database.ExecuteSqlRaw($"CREATE SCHEMA IF NOT EXISTS {PeopleManagementContext.DEFAULT_SCHEMA}"); //Se o migration já foi aplicado não tem o porque criar um schema que já foi teoricamente criado no migration.

// Configure the HTTP request pipeline.
if (env != null && env.Equals("Development"))
{
    
    var log = services.GetRequiredService<ILogger<Program>>();
    await PopulateDb.Populate(context, log);

}


app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
    {
        RequireSsl = false,
        SslRedirect = false,
        LoginCaseSensitive = true,
        Users = new []
        {
            new BasicAuthAuthorizationUser
            {
                Login = builder.Configuration["BackgroundJobs:Dashboard:Login"],
                PasswordClear = builder.Configuration["BackgroundJobs:Dashboard:Password"]
            }
        }

    }) }
});






// Swagger é de desenvolvimento. Até 2026-09-04 o documento e a UI subiam em produção também —
// e o fallback de autorização não os alcança, porque UseSwagger é middleware e não endpoint com
// metadado de autorização. Em produção a superfície simplesmente deixa de existir.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

var jobScheduler = scope.ServiceProvider.GetRequiredService<HangfireJobRegister>();
jobScheduler.RegisterRecurringJobs();

app.UseHttpsRedirection();

// Cabeçalhos de segurança em toda resposta. nosniff é o que importa: os endpoints de documento
// servem o tipo de mídia que o REMETENTE declarou, e sem ele o navegador poderia "adivinhar"
// HTML num anexo e executá-lo. Os outros dois fecham o que uma API não precisa deixar aberto.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseCors();

// UseAuthentication ANTES de UseAuthorization: sem ele o User chega sem claims e toda policy
// reprova — ou pior, o guard de rota acha claim nenhum e o 403 lê como falta de permissão.
app.UseAuthentication();
app.UseAuthorization();

// Depois da autenticação, porque a partição do limitador é o sub do token.
app.UseApiRateLimiting();

app.MapControllers();




app.Run();


public partial class Program { }