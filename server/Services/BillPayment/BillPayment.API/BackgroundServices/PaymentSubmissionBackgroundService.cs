namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Application.Queries.PaymentOrders;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Options;

/// <summary>
/// A fila de submissão de pagamentos — o único lugar do BC que manda dinheiro sair.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Só submete dentro da janela do ADR-017</strong> (9h–17h no fuso do provedor, por
/// configuração): fora dela o ciclo constata e volta a dormir, e o aluguel das ordens continua
/// livre para a primeira reivindicação de quando a janela abrir. Submissão em horário comercial
/// é submissão com gente acordada para reagir ao alerta.
/// </para>
/// <para>
/// <strong>Serial, de propósito.</strong> O gargalo não é o código: cada submissão é uma escrita
/// de dinheiro no provedor, e paralelizar multiplicaria o dano de qualquer defeito sistemático
/// antes de a primeira falha aparecer em log.
/// </para>
/// </remarks>
internal sealed class PaymentSubmissionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<PaymentSubmissionOptions> options,
    IOptions<PaymentSchedulingOptions> schedulingOptions,
    TimeProvider clock,
    ILogger<PaymentSubmissionBackgroundService> logger) : BackgroundService
{
    private readonly PaymentSubmissionOptions _options = options.Value;
    private readonly PaymentSchedulingOptions _scheduling = schedulingOptions.Value;

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
                logger.LogError(ex, "Payment submission cycle failed; will retry on next tick.");
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
        // A reconferência das ordens sem conta roda SEMPRE — liberar retenção não é submeter,
        // e esperar a janela para destravar só atrasaria a primeira submissão possível.
        await ProbeAccountHeldAsync(stoppingToken);

        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(
            clock.GetUtcNow().UtcDateTime, _scheduling.ResolveTimeZone());

        if (!_scheduling.ToPolicy().IsWithinSubmissionWindow(TimeOnly.FromDateTime(nowLocal)))
            return;

        var claimed = await ClaimAsync(stoppingToken);

        foreach (var pending in claimed)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            await SubmitOneAsync(pending, stoppingToken);
        }
    }

    private async Task<IReadOnlyList<PendingPaymentSubmission>> ClaimAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IPaymentOrderWorkQueries>();

        var leaseUntil = clock.GetUtcNow().Add(_options.LeaseDuration);

        return await queries.ClaimPendingSubmissionsAsync(_options.BatchSize, leaseUntil, stoppingToken);
    }

    private async Task SubmitOneAsync(PendingPaymentSubmission pending, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(
                new SubmitPaymentOrderCommand(pending.TenantId, pending.PaymentOrderId), stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await RecordFailureAsync(pending, ex, stoppingToken);
        }
    }

    /// <summary>
    /// Anota a falha num escopo NOVO — o que estourou tem um DbContext com o agregado meio
    /// mutado — e decide se a ordem desiste. Desistir emite o evento de falha, que reflete no
    /// boleto e avisa: nunca silêncio.
    /// </summary>
    private async Task RecordFailureAsync(
        PendingPaymentSubmission pending, Exception failure, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var orders = scope.ServiceProvider.GetRequiredService<IPaymentOrderRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var order = await orders.GetAsync(
                TenantId.From(pending.TenantId),
                PaymentOrderId.From(pending.PaymentOrderId),
                stoppingToken);

            if (order is null || order.Status != PaymentOrderStatus.Draft)
                return;

            var gaveUp = order.RecordSubmissionFailure(
                IsPermanent(failure),
                failure.Message,
                _scheduling.MaxSubmissionAttempts,
                TimeSpan.FromSeconds(Math.Max(1, _scheduling.RetryBaseDelaySeconds)),
                clock.GetUtcNow().UtcDateTime);

            await unitOfWork.SaveEntitiesAsync(stoppingToken);

            if (gaveUp)
                logger.LogWarning(failure, "A submissão desta ordem de pagamento desistiu após as tentativas.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Falhar ao registrar a falha não derruba o ciclo: o aluguel vence sozinho e a
            // ordem volta à fila.
            logger.LogError(ex, "Não foi possível registrar a falha da submissão de pagamento.");
        }
    }

    /// <summary>
    /// Só o sinal de "volte para a fila" (<c>BLP.PMO18</c>) e a colisão de concorrência são
    /// passageiros. O resto é permanente — e permanente aqui é o lado seguro, porque toda
    /// retentativa recomeça pela consulta de <c>externalReference</c>, nunca por reenvio cego.
    /// </summary>
    private static bool IsPermanent(Exception failure)
        => failure switch
        {
            ConcurrencyConflictException => false,
            DomainException domain => !string.Equals(
                domain.Id, SubmissionUnavailableErrorId, StringComparison.Ordinal),
            _ => true,
        };

    /// <summary><c>PaymentOrderErrors.SubmissionUnavailable</c>.</summary>
    private const string SubmissionUnavailableErrorId = "BLP.PMO18";

    private async Task ProbeAccountHeldAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IPaymentOrderWorkQueries>();

        var held = await queries.ListAccountHeldAsync(_options.AccountHeldProbeBatchSize, stoppingToken);

        foreach (var order in held)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            using var commandScope = scopeFactory.CreateScope();
            var mediator = commandScope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                await mediator.Send(
                    new ReleasePaymentOrderAccountHoldCommand(order.TenantId, order.PaymentOrderId),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Uma ordem que não destrava não pode impedir as outras de destravar.
                logger.LogError(ex, "Não foi possível reconferir uma ordem retida por falta de conta.");
            }
        }
    }
}
