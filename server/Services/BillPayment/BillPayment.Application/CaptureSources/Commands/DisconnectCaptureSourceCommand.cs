namespace BillPayment.Application.CaptureSources.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Desconecta a fonte e apaga a credencial do cofre.
/// </summary>
/// <remarks>
/// Os <c>CaptureItem</c> já ingeridos <strong>não</strong> são apagados junto: eles são a
/// trilha de auditoria de tudo que entrou, inclusive dos boletos que já viraram <c>Bill</c> e
/// podem ter sido pagos. Apagá-los deixaria pagamento sem procedência.
/// </remarks>
public sealed record DisconnectCaptureSourceCommand(
    Guid TenantId,
    Guid CaptureSourceId) : ITenantScopedCommand, IRequest<DisconnectCaptureSourceResponse>;

public sealed record DisconnectCaptureSourceResponse(Guid Id);

public sealed class DisconnectCaptureSourceCommandHandler(
    ICaptureSourceRepository repository,
    ISecretVault vault,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DisconnectCaptureSourceCommand, DisconnectCaptureSourceResponse>
{
    public async Task<DisconnectCaptureSourceResponse> Handle(
        DisconnectCaptureSourceCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CaptureSourceId.From(request.CaptureSourceId);

        var source = await repository.GetAsync(tenantId, id, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        if (source.Credential is not null)
            await vault.RemoveAsync(source.Credential, cancellationToken);

        repository.Remove(source);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new DisconnectCaptureSourceResponse(request.CaptureSourceId);
    }
}

public sealed class DisconnectCaptureSourceIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<DisconnectCaptureSourceIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<DisconnectCaptureSourceCommand, DisconnectCaptureSourceResponse>(
        mediator, requestManager, logger)
{
    protected override DisconnectCaptureSourceResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
