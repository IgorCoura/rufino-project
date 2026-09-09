namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.Mediator;
using BillPayment.Application.PayerProfiles.Commands;
using BillPayment.Application.Queries.PayerProfiles;
using Microsoft.Extensions.Options;

/// <summary>
/// A reconciliação de ASSINATURA: confere de tempos em tempos se o webhook de cada tenant existe
/// e está entregando, e conserta o que não estiver.
/// </summary>
/// <remarks>
/// <para>
/// Irmã da <c>PaymentReconciliationBackgroundService</c>, e as duas juntas são o padrão completo
/// que a indústria consolidou para webhook: o evento avisa rápido, a releitura na API diz a
/// verdade, e a varredura periódica acha o que se perdeu. A conciliação cuida das ORDENS; esta
/// cuida do CANAL por onde as notícias delas chegam.
/// </para>
/// <para>
/// <strong>Também roda uma vez no arranque</strong>, e não só no intervalo — a lição do
/// <c>refresh-webhook-job</c> do PeopleManagement: se a instalação passou a janela de
/// indisponibilidade do provedor desligada, esperar meia hora para descobrir é meia hora a mais
/// de dinheiro andando sem ninguém saber.
/// </para>
/// </remarks>
internal sealed class PaymentWebhookSweepBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<PaymentWebhookSweepOptions> options,
    ILogger<PaymentWebhookSweepBackgroundService> logger) : BackgroundService
{
    private readonly PaymentWebhookSweepOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "O ciclo de varredura de webhooks falhou; nova tentativa no próximo tick.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        IReadOnlyList<Guid> tenantIds;

        using (var scope = scopeFactory.CreateScope())
        {
            var queries = scope.ServiceProvider.GetRequiredService<IPayerProfileWorkQueries>();
            var targets = await queries.ListWithLinkedAccountAsync(_options.BatchSize, stoppingToken);

            tenantIds = [.. targets.Select(t => t.TenantId)];
        }

        foreach (var tenantId in tenantIds)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                await mediator.Send(new SweepPaymentWebhookCommand(tenantId), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Um tenant que não se deixa varrer não pode tirar os outros da varredura — é a
                // mesma doutrina do laço da conciliação.
                logger.LogError(
                    ex, "Não foi possível varrer o webhook do tenant {TenantId}.", tenantId);
            }
        }
    }
}
