namespace BillPayment.Application.CaptureItems.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Uma pessoa olhou o item da quarentena e disse que não reconhece.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É a operação que torna a fila esvaziável.</strong> Sem ela, todo item que a cascata
/// não resolve fica pendente para sempre e a quarentena vira uma lista que ninguém termina de
/// olhar — o mesmo destino que a doutrina de "descartar é o padrão" existe para evitar, chegando
/// pelo outro lado.
/// </para>
/// <para>
/// <strong>É <c>IMultiAggregateCommand</c></strong> porque o livro-caixa da captura precisa
/// contar a mesma história: um item reprovado cujo registro ainda diga "aguardando" mandaria a
/// tela de e-mails e a de quarentena discordarem sobre o mesmo documento.
/// </para>
/// </remarks>
/// <param name="Note">
/// Observação livre de quem reprovou. Opcional de propósito: exigir justificativa transforma
/// uma decisão de dois segundos numa de trinta, e a fila deixa de ser esvaziável na prática.
/// </param>
public sealed record DismissCaptureItemCommand(
    Guid TenantId,
    Guid CaptureItemId,
    Guid DismissedBy,
    string? Note) : IRequest<DismissCaptureItemResponse>, IMultiAggregateCommand;

public sealed record DismissCaptureItemResponse(Guid Id, string PreviousStatus);

public sealed class DismissCaptureItemCommandHandler(
    ICaptureItemRepository items,
    ICapturedMessageRepository capturedMessages,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<DismissCaptureItemCommandHandler> logger)
    : IRequestHandler<DismissCaptureItemCommand, DismissCaptureItemResponse>
{
    public async Task<DismissCaptureItemResponse> Handle(
        DismissCaptureItemCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var itemId = CaptureItemId.From(request.CaptureItemId);

        var item = await items.GetAsync(tenantId, itemId, cancellationToken)
            ?? throw CaptureItemErrors.NotFound(request.CaptureItemId);

        var previous = item.Status.Name;
        var now = clock.GetUtcNow().UtcDateTime;

        // Quem recusa reprovar o que não pode ser reprovado é o agregado: item já promovido a
        // boleto, ou já atribuído a outro pagador, não sai da vista por decisão deste tenant.
        item.Dismiss(UserId.From(request.DismissedBy), request.Note, now);

        var message = await capturedMessages.FindByExternalMessageIdAsync(
            tenantId, item.SourceId, item.ExternalMessageId, cancellationToken);

        message?.RecordOutcome(
            item.ArtifactKey, ArtifactOutcome.Dismissed, item.Reason, item.Id, item.BillId, now);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Capture item {ItemId} dismissed by a user (was {PreviousStatus}).",
                item.Id.Value,
                previous);
        }

        return new DismissCaptureItemResponse(item.Id.Value, previous);
    }
}

public sealed class DismissCaptureItemIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<DismissCaptureItemIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<DismissCaptureItemCommand, DismissCaptureItemResponse>(
        mediator, requestManager, logger)
{
    protected override DismissCaptureItemResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty);
}
