namespace BillPayment.Application.Expectations.EventHandlers;

using BillPayment.Application.Expectations.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
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
/// Um artefato chegou e travou — procura a conta esperada que ele estava vindo cumprir.
/// </summary>
/// <remarks>
/// É o elo que faltava para o alerta de "chegou e não consegui ler" existir: até 2026-08-27 o
/// <c>CaptureItem</c> não emitia evento nenhum, e o método que marca o ciclo como parcialmente
/// capturado não era chamado por código de produção nenhum.
/// </remarks>
public sealed class RecordCaptureFailureOnItemStuckHandler(IMediator mediator)
    : IDomainEventHandler<CaptureItemStuckDomainEvent>
{
    public async Task HandleAsync(
        CaptureItemStuckDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new RecordExpectationCaptureFailureCommand(
                domainEvent.TenantId.Value,
                domainEvent.CaptureItemId.Value,
                domainEvent.SourceId.Value,
                domainEvent.Status,
                domainEvent.ReceivedAt),
            cancellationToken);
    }
}

/// <summary>O artefato preso foi resolvido — o ciclo para de apontar para ele.</summary>
public sealed class ClearCaptureFailureOnItemUnstuckHandler(IMediator mediator)
    : IDomainEventHandler<CaptureItemUnstuckDomainEvent>
{
    public async Task HandleAsync(
        CaptureItemUnstuckDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new ClearExpectationCaptureFailureCommand(
                domainEvent.TenantId.Value, domainEvent.CaptureItemId.Value),
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

/// <summary>
/// Um nível do escalonamento saiu — é este handler que leva o aviso ao usuário.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Substituiu o aviso pendurado na transição para <c>Missing</c>.</strong> Aquela
/// acontece uma vez por ciclo; o escalonamento acontece quatro. Enquanto o aviso saía de lá,
/// <c>Warning</c>, <c>Urgent</c> e <c>Overdue</c> ficavam gravados no agregado e nunca chegavam
/// a ninguém — a tabela de escalonamento do doc 11 existia só no papel.
/// </para>
/// <para>
/// <strong>O texto muda por nível E por família de motivo.</strong> "Não chegou, vá buscar" e
/// "chegou e não consegui ler, resolva o item" pedem ações opostas, e é essa distinção que dá
/// utilidade ao alerta — quem decide é <c>MissReason.Arrived</c>, projetado no evento.
/// </para>
/// </remarks>
public sealed class NotifyExpectationAlertRaisedHandler(INotificationSender notifications)
    : IDomainEventHandler<BillExpectationAlertRaisedDomainEvent>
{
    public async Task HandleAsync(
        BillExpectationAlertRaisedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var kind = domainEvent.Arrived
            ? NotificationKind.ExpectationCaptureFailed
            : NotificationKind.ExpectationMissing;

        // O aviso do artefato leva ao item resolvível; o da conta ausente, à expectativa.
        var path = domainEvent.Arrived && domainEvent.CaptureItemId is { } itemId
            ? $"/capture-items/{itemId}"
            : $"/expectations/{domainEvent.ExpectationId.Value}";

        await notifications.SendAsync(
            domainEvent.TenantId,
            kind,
            new NotificationPayload(
                $"{Urgency(domainEvent.Level)}: {domainEvent.Label} ({domainEvent.Competence})",
                Body(domainEvent),
                path),
            cancellationToken);
    }

    private static string Urgency(string level) => level switch
    {
        nameof(AlertLevel.HeadsUp) => "Aviso",
        nameof(AlertLevel.Warning) => "Atenção",
        nameof(AlertLevel.Urgent) => "Vence hoje",
        nameof(AlertLevel.Overdue) => "Vencida",
        _ => "Aviso",
    };

    private static string Body(BillExpectationAlertRaisedDomainEvent e)
    {
        var due = e.ExpectedDueDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

        var situation = e.Arrived
            ? $"A conta chegou e não consegui ler: {e.MissReason}. O documento está guardado — "
                + "abra o item para informar a senha, reivindicar ou digitar a linha à mão."
            : "Passou da data em que ela costuma chegar e nada foi capturado. "
                + "Busque no portal do emissor ou confira a caixa de e-mail.";

        var pressure = e.Level switch
        {
            nameof(AlertLevel.Urgent) => $" O vencimento é hoje ({due}).",
            nameof(AlertLevel.Overdue) => $" O vencimento era {due} e há encargos correndo.",
            _ => $" O vencimento previsto é {due}.",
        };

        return situation + pressure;
    }
}

/// <summary>
/// A conta chegou e não deu para ler — o aviso imediato, no instante em que o artefato trava.
/// </summary>
/// <remarks>
/// Convive com o escalonamento e não o duplica: este sai <strong>uma vez</strong>, quando o item
/// trava, e é o mais acionável de todos porque o sistema acabou de descobrir exatamente o que
/// falta. Os níveis seguintes saem pelo escalonamento, se ninguém resolver.
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
