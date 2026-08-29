using BillPayment.API.Authentication;
using BillPayment.API.Authorization;
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

// Autenticação e autorização iguais às dos outros dois BCs: JWT do Keycloak + policy montada em
// tempo de execução a partir de [ProtectedResource]. Os papéis vivem no realm, não aqui.
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization(builder.Configuration);
builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApiWithBearer();

// Agendador de captura. Desligado por padrão e registrado só quando Capture:Enabled=true — sem
// adapter de provedor ele só produziria falhas registradas em toda fonte, e a suíte de
// integração dirige a sincronização de forma determinística pelo endpoint manual.
builder.Services.Configure<CaptureSyncOptions>(builder.Configuration.GetSection(CaptureSyncOptions.SectionName));

if (builder.Configuration.GetValue<bool>($"{CaptureSyncOptions.SectionName}:Enabled"))
{
    builder.Services.AddHostedService<CaptureSyncBackgroundService>();

    // Varrer caixa e processar artefato tem ritmos e modos de falha diferentes — um anexo lento
    // nao pode atrasar a varredura, que e o que garante que nada fica para tras.
    builder.Services.AddHostedService<CaptureProcessingBackgroundService>();

    // Terceiro worker: a faixa lenta. Só ele gasta cota de IA, e é isso que impede um artefato
    // de 5 segundos de segurar um lote cujo item mediano leva 150 ms.
    builder.Services.AddHostedService<CaptureVisionBackgroundService>();
}

// A varredura de expectativas é LIGADA por padrão, ao contrário da captura. A captura desligada
// apenas não captura; a expectativa desligada desliga a rede de segurança — e o modo de falha
// dela é o silêncio, que é o que o ADR-014 existe para evitar. Sem expectativa cadastrada o
// ciclo não faz nada e não custa nada.
builder.Services.Configure<ExpectationSweepOptions>(
    builder.Configuration.GetSection(ExpectationSweepOptions.SectionName));

if (builder.Configuration.GetValue<bool?>($"{ExpectationSweepOptions.SectionName}:Enabled") ?? true)
    builder.Services.AddHostedService<ExpectationSweepBackgroundService>();

// O worker vem ligado; quem decide se algum registro é apagado é a política de cada tenant, que
// nasce desligada. Desligar o worker por padrão faria a política ligada não valer nada.
// A fila da leitura por IA dos boletos. Ligada por padrão: um boleto que nasce "Na fila para
// análise" e nunca sai é pior que não ter análise nenhuma.
builder.Services.Configure<BillReadingOptions>(
    builder.Configuration.GetSection(BillReadingOptions.SectionName));

if (builder.Configuration.GetValue<bool?>($"{BillReadingOptions.SectionName}:Enabled") ?? true)
    builder.Services.AddHostedService<BillReadingBackgroundService>();

builder.Services.Configure<CaptureRetentionOptions>(
    builder.Configuration.GetSection(CaptureRetentionOptions.SectionName));

if (builder.Configuration.GetValue<bool?>($"{CaptureRetentionOptions.SectionName}:Enabled") ?? true)
    builder.Services.AddHostedService<CaptureRetentionBackgroundService>();

var app = builder.Build();

// Migrações, não EnsureCreatedAsync. A diferença não é estilística: EnsureCreated decide por
// "o banco tem alguma tabela?", não por "o schema bate com o modelo?" — então um Aggregate novo
// nunca ganhava tabela num banco já existente, a aplicação subia com êxito, e a falha só
// aparecia na primeira consulta como 42P01. Aconteceu de verdade em 2026-08-11 (gotchas.md).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillPaymentDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    // AllowAnonymous explícito: desde 2026-08-28 o fallback de autorização exige autenticação em
    // todo endpoint sem atributo, e o documento OpenAPI precisa continuar acessível ao Swagger UI.
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BillPayment API");
    });

    app.MapGet("/", () => Results.LocalRedirect("~/swagger")).ExcludeFromDescription().AllowAnonymous();
}
else
{
    app.UseHsts();
}

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

await app.RunAsync();

public partial class Program;
