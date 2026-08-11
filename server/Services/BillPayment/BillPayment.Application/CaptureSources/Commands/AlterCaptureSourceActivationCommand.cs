namespace BillPayment.Application.CaptureSources.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Liga e desliga a captura desta fonte. É o botão de parada do usuário — uma fonte desativada
/// recusa a sincronização no próprio agregado (<c>BLP.CPS12</c>), não por filtro de consulta.
/// </summary>
public sealed record AlterCaptureSourceActivationCommand(
    Guid TenantId,
    Guid CaptureSourceId,
    bool IsEnabled) : IRequest<AlterCaptureSourceActivationResponse>;

public sealed record AlterCaptureSourceActivationResponse(Guid Id);

public sealed class AlterCaptureSourceActivationCommandHandler(
    ICaptureSourceRepository repository,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AlterCaptureSourceActivationCommand, AlterCaptureSourceActivationResponse>
{
    public async Task<AlterCaptureSourceActivationResponse> Handle(
        AlterCaptureSourceActivationCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CaptureSourceId.From(request.CaptureSourceId);

        var source = await repository.GetAsync(tenantId, id, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        source.SetEnabled(request.IsEnabled, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AlterCaptureSourceActivationResponse(source.Id.Value);
    }
}

public sealed class AlterCaptureSourceActivationIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AlterCaptureSourceActivationIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AlterCaptureSourceActivationCommand, AlterCaptureSourceActivationResponse>(
        mediator, requestManager, logger)
{
    protected override AlterCaptureSourceActivationResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
