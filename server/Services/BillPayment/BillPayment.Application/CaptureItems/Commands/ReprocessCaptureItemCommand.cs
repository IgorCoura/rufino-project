namespace BillPayment.Application.CaptureItems.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Devolve um artefato já triado à fila, para a cascata de hoje avaliá-lo de novo.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque o desfecho de um artefato é do dia em que ele passou.</strong> A cascata
/// ganhou um degrau (a visão, na 2.4) e vai ganhar outros; o prompt muda; o cadastro muda — sem
/// <c>PayerProfile</c> não há senha derivada, e sem <c>Payee</c> nem <c>TrustedOrigin</c> o que o
/// parser erra é descartado. Antes disto, reavaliar exigia apagar linhas no banco à mão, e foi
/// exatamente o que travou a primeira medição da 2.4.
/// </para>
/// <para>
/// <strong>Não reprocessa aqui.</strong> Só reabre: o item volta a <c>Received</c> e o worker de
/// processamento faz o resto, pelo mesmo caminho do primeiro processamento. Um segundo caminho
/// seria um segundo lugar para as regras envelhecerem — e este comando ficaria devendo o
/// download, a extração, a triagem e a retenção por desfecho.
/// </para>
/// <para>
/// <strong>Reabrir um por vez é deliberado.</strong> A visão custa por documento e a conta tem
/// teto diário: reabrir a quarentena inteira de uma vez queimaria a cota antes de chegar nos
/// itens que interessam. Quem opera escolhe quais valem.
/// </para>
/// </remarks>
public sealed record ReprocessCaptureItemCommand(Guid TenantId, Guid CaptureItemId)
    : ITenantScopedCommand, IRequest<ReprocessCaptureItemResponse>;

/// <param name="PreviousStatus">De onde ele voltou — para a tela dizer o que mudou.</param>
public sealed record ReprocessCaptureItemResponse(Guid Id, string PreviousStatus);

public sealed class ReprocessCaptureItemCommandHandler(
    ICaptureItemRepository items,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReprocessCaptureItemCommand, ReprocessCaptureItemResponse>
{
    public async Task<ReprocessCaptureItemResponse> Handle(
        ReprocessCaptureItemCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var itemId = CaptureItemId.From(request.CaptureItemId);

        var item = await items.GetAsync(tenantId, itemId, cancellationToken)
            ?? throw CaptureItemErrors.NotFound(request.CaptureItemId);

        var previous = item.Status.Name;

        // Quem recusa reabrir o que não pode ser reaberto é o agregado: item já promovido a
        // boleto, ou descartado, não volta para a fila (BLP.CPI03).
        item.Reopen(clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReprocessCaptureItemResponse(item.Id.Value, previous);
    }
}

public sealed class ReprocessCaptureItemIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ReprocessCaptureItemIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ReprocessCaptureItemCommand, ReprocessCaptureItemResponse>(
        mediator, requestManager, logger)
{
    protected override ReprocessCaptureItemResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty);
}
