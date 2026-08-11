namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tira o boleto do fluxo. Diferente de recusar: alcança documento que nem chegou a ser
/// verificado, e é o que resolve boleto importado por engano.
/// </summary>
public sealed record CancelBillCommand(
    Guid TenantId,
    Guid BillId,
    Guid UserId,
    string Reason) : IRequest<CancelBillResponse>;

public sealed record CancelBillResponse(Guid Id, string Status);

public sealed class CancelBillCommandHandler(
    IBillRepository bills,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelBillCommand, CancelBillResponse>
{
    public async Task<CancelBillResponse> Handle(CancelBillCommand request, CancellationToken cancellationToken)
    {
        var bill = await bills.GetAsync(
            TenantId.From(request.TenantId), BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        bill.Cancel(UserId.From(request.UserId), request.Reason, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new CancelBillResponse(bill.Id.Value, bill.Status.Name);
    }
}

public sealed class CancelBillIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<CancelBillIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<CancelBillCommand, CancelBillResponse>(mediator, requestManager, logger)
{
    protected override CancelBillResponse CreateResultForDuplicateRequest() => new(Guid.Empty, string.Empty);
}
