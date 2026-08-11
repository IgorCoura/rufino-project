namespace BillPayment.Application.CaptureSources.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record RenameCaptureSourceCommand(
    Guid TenantId,
    Guid CaptureSourceId,
    string DisplayName) : IRequest<RenameCaptureSourceResponse>;

public sealed record RenameCaptureSourceResponse(Guid Id);

public sealed class RenameCaptureSourceCommandHandler(
    ICaptureSourceRepository repository,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RenameCaptureSourceCommand, RenameCaptureSourceResponse>
{
    public async Task<RenameCaptureSourceResponse> Handle(
        RenameCaptureSourceCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CaptureSourceId.From(request.CaptureSourceId);

        var source = await repository.GetAsync(tenantId, id, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        source.Rename(request.DisplayName, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RenameCaptureSourceResponse(source.Id.Value);
    }
}

public sealed class RenameCaptureSourceIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RenameCaptureSourceIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RenameCaptureSourceCommand, RenameCaptureSourceResponse>(mediator, requestManager, logger)
{
    protected override RenameCaptureSourceResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
