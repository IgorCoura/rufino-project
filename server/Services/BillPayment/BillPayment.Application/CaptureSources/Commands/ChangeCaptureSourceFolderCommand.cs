namespace BillPayment.Application.CaptureSources.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Aponta a fonte para outra pasta da caixa — ou de volta para a caixa de entrada, com nulo.
/// </summary>
/// <remarks>
/// Existe como operação própria, e não só no <c>Connect</c>, porque trocar a pasta pelo caminho
/// de reconectar obrigaria o usuário a digitar a credencial de novo — e credencial digitada à
/// toa é credencial que circula à toa.
/// </remarks>
public sealed record ChangeCaptureSourceFolderCommand(
    Guid TenantId,
    Guid CaptureSourceId,
    string? FolderPath) : IRequest<ChangeCaptureSourceFolderResponse>;

public sealed record ChangeCaptureSourceFolderResponse(Guid Id);

public sealed class ChangeCaptureSourceFolderCommandHandler(
    ICaptureSourceRepository repository,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeCaptureSourceFolderCommand, ChangeCaptureSourceFolderResponse>
{
    public async Task<ChangeCaptureSourceFolderResponse> Handle(
        ChangeCaptureSourceFolderCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CaptureSourceId.From(request.CaptureSourceId);

        var source = await repository.GetAsync(tenantId, id, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        // Quem descarta o cursor junto é o agregado — a varredura incremental é por pasta, e
        // manter o cursor faria a primeira leitura da pasta nova voltar vazia.
        source.ChangeFolder(request.FolderPath, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ChangeCaptureSourceFolderResponse(source.Id.Value);
    }
}

public sealed class ChangeCaptureSourceFolderIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ChangeCaptureSourceFolderIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ChangeCaptureSourceFolderCommand, ChangeCaptureSourceFolderResponse>(
        mediator, requestManager, logger)
{
    protected override ChangeCaptureSourceFolderResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
