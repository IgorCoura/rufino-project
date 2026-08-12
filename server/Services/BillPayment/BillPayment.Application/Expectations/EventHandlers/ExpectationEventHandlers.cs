namespace BillPayment.Application.Expectations.EventHandlers;

using BillPayment.Application.Expectations.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;

/// <summary>
/// Boleto verificado procura o ciclo de expectativa que ele cumpre.
/// </summary>
/// <remarks>
/// Depois da verificação, e não da captura, porque é ela que resolve o beneficiário — sem ele
/// não há contra o quê casar. O outbox entrega ao-menos-uma-vez e o comando é idempotente: um
/// ciclo já cumprido não é cumprido de novo.
/// </remarks>
public sealed class FulfillExpectationOnBillValidatedHandler(IMediator mediator)
    : IDomainEventHandler<BillValidatedDomainEvent>
{
    public async Task HandleAsync(
        BillValidatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new FulfillExpectationForBillCommand(domainEvent.TenantId.Value, domainEvent.BillId.Value),
            cancellationToken);
    }
}

/// <summary>
/// Boleto aprovado alimenta o aprendizado do beneficiário.
/// </summary>
/// <remarks>
/// A aprovação é o momento em que a ocorrência vira confiável: boleto capturado ainda pode ser
/// recusado, e aprender de um documento que a pessoa rejeitou criaria expectativa de uma conta
/// que ela não reconhece.
/// </remarks>
public sealed class LearnExpectationOnBillApprovedHandler(IMediator mediator, IBillRepository bills)
    : IDomainEventHandler<BillApprovedDomainEvent>
{
    public async Task HandleAsync(
        BillApprovedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var bill = await bills.GetAsync(domainEvent.TenantId, domainEvent.BillId, cancellationToken);

        // Sem beneficiário resolvido não há agrupamento possível — e é desfecho normal, não erro.
        if (bill?.PayeeId is not { } payeeId)
            return;

        await mediator.Send(
            new LearnBillExpectationsCommand(domainEvent.TenantId.Value, payeeId.Value),
            cancellationToken);
    }
}

/// <summary>
/// O sistema passou a monitorar uma conta por conta própria, e avisa.
/// </summary>
/// <remarks>
/// Criar a expectativa em silêncio seria pior que não criá-la: a primeira notícia da existência
/// dela seria um alerta que o usuário não pediu.
/// </remarks>
public sealed class NotifyExpectationLearnedHandler(INotificationSender notifications)
    : IDomainEventHandler<BillExpectationLearnedDomainEvent>
{
    public async Task HandleAsync(
        BillExpectationLearnedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await notifications.SendAsync(
            domainEvent.TenantId,
            NotificationKind.ExpectationLearned,
            new NotificationPayload(
                $"Passei a monitorar {domainEvent.Label}",
                $"Notei que esta conta chega com regularidade ({domainEvent.Recurrence.ToLowerInvariant()}) "
                + "e vou avisar se algum mês ela não chegar. Se não quiser, é só desativar.",
                $"/expectations/{domainEvent.ExpectationId.Value}"),
            cancellationToken);
    }
}

/// <summary>A conta não chegou — o aviso manda buscar.</summary>
public sealed class NotifyExpectationMissedHandler(INotificationSender notifications)
    : IDomainEventHandler<BillExpectationMissedDomainEvent>
{
    public async Task HandleAsync(
        BillExpectationMissedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await notifications.SendAsync(
            domainEvent.TenantId,
            NotificationKind.ExpectationMissing,
            new NotificationPayload(
                "Uma conta esperada não chegou",
                "Passou da data em que ela costuma chegar e nada foi capturado. "
                + "Busque no portal do emissor ou confira a caixa de e-mail.",
                $"/expectations/{domainEvent.ExpectationId.Value}"),
            cancellationToken);
    }
}

/// <summary>
/// A conta chegou e não deu para ler — o aviso leva ao item resolvível.
/// </summary>
/// <remarks>
/// É o mais valioso dos dois avisos: o sistema já tem o documento e sabe exatamente o que falta,
/// então a ação oferecida é informar a senha, reivindicar ou digitar a linha — um clique, não
/// uma busca.
/// </remarks>
public sealed class NotifyExpectationCaptureFailedHandler(INotificationSender notifications)
    : IDomainEventHandler<BillExpectationCaptureFailedDomainEvent>
{
    public async Task HandleAsync(
        BillExpectationCaptureFailedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await notifications.SendAsync(
            domainEvent.TenantId,
            NotificationKind.ExpectationCaptureFailed,
            new NotificationPayload(
                "Uma conta esperada chegou e não consegui ler",
                $"O documento está guardado, e o motivo foi: {domainEvent.Reason}. "
                + "Abra o item para informar a senha, reivindicar ou digitar a linha à mão.",
                $"/capture-items/{domainEvent.CaptureItemId.Value}"),
            cancellationToken);
    }
}
