namespace BillPayment.Application.CaptureSources.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Move o piso temporal da captura — a data antes da qual a caixa não é lida.
/// </summary>
/// <remarks>
/// <para>
/// Existe como operação própria, e não só no <c>Connect</c>, pelo mesmo motivo de
/// <c>ChangeCaptureSourceFolderCommand</c>: ajustar o piso pelo caminho de reconectar obrigaria
/// o usuário a digitar a credencial de novo, e credencial digitada à toa é credencial que
/// circula à toa.
/// </para>
/// <para>
/// <strong>Quem descarta os cursores é o agregado</strong>, não este handler. O provedor grava o
/// filtro dentro do <c>deltaLink</c> que devolve, então um cursor velho continuaria mandando a
/// data velha — e a troca não valeria nada, em silêncio.
/// </para>
/// </remarks>
/// <param name="CaptureSince">Nulo devolve a fonte à caixa inteira.</param>
public sealed record ChangeCaptureSourceSinceCommand(
    Guid TenantId,
    Guid CaptureSourceId,
    DateOnly? CaptureSince) : ITenantScopedCommand, IRequest<ChangeCaptureSourceSinceResponse>;

public sealed record ChangeCaptureSourceSinceResponse(Guid Id);

public sealed class ChangeCaptureSourceSinceCommandHandler(
    ICaptureSourceRepository repository,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeCaptureSourceSinceCommand, ChangeCaptureSourceSinceResponse>
{
    public async Task<ChangeCaptureSourceSinceResponse> Handle(
        ChangeCaptureSourceSinceCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CaptureSourceId.From(request.CaptureSourceId);

        var source = await repository.GetAsync(tenantId, id, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        source.ChangeCaptureSince(request.CaptureSince, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ChangeCaptureSourceSinceResponse(source.Id.Value);
    }
}

public sealed class ChangeCaptureSourceSinceIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ChangeCaptureSourceSinceIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ChangeCaptureSourceSinceCommand, ChangeCaptureSourceSinceResponse>(
        mediator, requestManager, logger)
{
    protected override ChangeCaptureSourceSinceResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
