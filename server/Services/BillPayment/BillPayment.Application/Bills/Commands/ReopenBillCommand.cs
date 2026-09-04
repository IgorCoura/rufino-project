namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Devolve um boleto de pagamento FALHADO à fila de decisão. A nova tentativa é uma nova
/// aprovação e uma nova ordem (ADR-002) — a falhada fica como história, na fila operacional.
/// </summary>
public sealed record ReopenBillCommand(Guid TenantId, Guid BillId)
    : ITenantScopedCommand, IRequest<ReopenBillResponse>;

public sealed record ReopenBillResponse(Guid BillId, string Status);

public sealed class ReopenBillCommandHandler(
    IBillRepository bills,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReopenBillCommand, ReopenBillResponse>
{
    public async Task<ReopenBillResponse> Handle(ReopenBillCommand request, CancellationToken cancellationToken)
    {
        var bill = await bills.GetAsync(
                TenantId.From(request.TenantId), BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        bill.ReopenForApproval(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReopenBillResponse(bill.Id.Value, bill.Status.Name);
    }
}

public sealed class ReopenBillIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ReopenBillIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ReopenBillCommand, ReopenBillResponse>(mediator, requestManager, logger)
{
    protected override ReopenBillResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty);
}
