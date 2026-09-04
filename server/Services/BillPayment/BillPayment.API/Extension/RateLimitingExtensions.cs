namespace BillPayment.API.Extension;

using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Teto de requisições por pessoa. Dois limitadores: um global, folgado, e o
/// <see cref="EXPENSIVE_POLICY"/> para o que custa dinheiro ou cota — consulta oficial no Asaas,
/// extração por IA, importação com PDF (até 40 senhas tentadas), varredura de caixa.
/// </summary>
/// <remarks>
/// <para>
/// A partição é o <c>sub</c> do token (ou o IP, para o que é anônimo). Não é defesa contra ataque
/// distribuído — é o que impede um único usuário, ou um cliente em laço, de esgotar a cota do
/// provedor de todos os tenants (o <c>MinIntervalMs</c> do extrator é global à instalação) ou de
/// pagar consulta oficial a cada clique em "revalidar".
/// </para>
/// <para>
/// <c>RateLimiting:Enabled=false</c> desliga tudo — é o que a suíte de integração faz, porque
/// os testes batem no mesmo usuário centenas de vezes por minuto. A prova de que o limitador
/// funciona vive num host próprio com teto de 2.
/// </para>
/// </remarks>
public static class RateLimitingExtensions
{
    public const string EXPENSIVE_POLICY = "expensive";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddSingleton(options);

        if (!options.Enabled)
            return services;

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = ((int)Window.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(PartitionKeyOf(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimitPerMinute,
                    Window = Window,
                    QueueLimit = 0,
                }));

            limiter.AddPolicy(EXPENSIVE_POLICY, context =>
                RateLimitPartition.GetFixedWindowLimiter(PartitionKeyOf(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.ExpensivePermitLimitPerMinute,
                    Window = Window,
                    QueueLimit = 0,
                }));
        });

        return services;
    }

    public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.ApplicationServices.GetRequiredService<RateLimitingOptions>();
        return options.Enabled ? app.UseRateLimiter() : app;
    }

    // A pessoa, não o tenant: uma conta com dez usuários não pode ser derrubada por um deles, e
    // um usuário com acesso a dois tenants não ganha o dobro da cota trocando de rota.
    private static string PartitionKeyOf(HttpContext context)
        => context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>Qualquer requisição autenticada, por pessoa e por minuto.</summary>
    public int PermitLimitPerMinute { get; set; } = 300;

    /// <summary>Requisições que gastam provedor externo, por pessoa e por minuto.</summary>
    public int ExpensivePermitLimitPerMinute { get; set; } = 30;
}
