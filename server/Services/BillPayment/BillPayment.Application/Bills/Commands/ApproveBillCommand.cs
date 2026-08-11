namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Um humano autoriza o pagamento e escolhe a data.</summary>
public sealed record ApproveBillCommand(
    Guid TenantId,
    Guid BillId,
    Guid UserId,
    DateOnly ScheduleFor,
    string? Note) : IRequest<ApproveBillResponse>;

public sealed record ApproveBillResponse(Guid Id, string Status, DateOnly ScheduledFor);

public sealed class ApproveBillCommandHandler(
    IBillRepository bills,
    IOptions<ApprovalOptions> options,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApproveBillCommand, ApproveBillResponse>
{
    public async Task<ApproveBillResponse> Handle(ApproveBillCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var bill = await bills.GetAsync(tenantId, BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        var now = clock.GetUtcNow();

        // Todas as guardas — cobertura de checks, bloqueio, validade do retrato, data e alçada —
        // vivem dentro do método rico. O handler resolve a política e a data de hoje, e nada mais.
        bill.Approve(
            UserId.From(request.UserId),
            request.ScheduleFor,
            request.Note,
            options.Value.ToPolicy(),
            DateOnly.FromDateTime(now.UtcDateTime),
            now.UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ApproveBillResponse(bill.Id.Value, bill.Status.Name, bill.ScheduledFor!.Value);
    }
}

public sealed class ApproveBillIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ApproveBillIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ApproveBillCommand, ApproveBillResponse>(mediator, requestManager, logger)
{
    protected override ApproveBillResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, default);
}
