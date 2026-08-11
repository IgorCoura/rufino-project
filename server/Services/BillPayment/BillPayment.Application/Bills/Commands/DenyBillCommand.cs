namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// O humano recusa o boleto. Terminal: recusar libera a chave natural e o documento pode ser
/// reimportado se o problema for resolvido na origem.
/// </summary>
public sealed record DenyBillCommand(
    Guid TenantId,
    Guid BillId,
    Guid UserId,
    string Reason) : IRequest<DenyBillResponse>;

public sealed record DenyBillResponse(Guid Id, string Status);

public sealed class DenyBillCommandHandler(
    IBillRepository bills,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DenyBillCommand, DenyBillResponse>
{
    public async Task<DenyBillResponse> Handle(DenyBillCommand request, CancellationToken cancellationToken)
    {
        var bill = await bills.GetAsync(
            TenantId.From(request.TenantId), BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        bill.Deny(UserId.From(request.UserId), request.Reason, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new DenyBillResponse(bill.Id.Value, bill.Status.Name);
    }
}

public sealed class DenyBillIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<DenyBillIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<DenyBillCommand, DenyBillResponse>(mediator, requestManager, logger)
{
    protected override DenyBillResponse CreateResultForDuplicateRequest() => new(Guid.Empty, string.Empty);
}
