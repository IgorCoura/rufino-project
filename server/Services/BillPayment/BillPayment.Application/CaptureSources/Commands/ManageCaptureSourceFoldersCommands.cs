namespace BillPayment.Application.CaptureSources.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Acrescenta uma pasta à lista que a fonte acompanha.
/// </summary>
/// <remarks>
/// <para>
/// Existe porque a delta query do provedor é <strong>por pasta</strong>: quem separa boleto em
/// "Contas" e nota fiscal em "Fiscal" precisava de duas fontes — duas credenciais, dois
/// cadastros — para uma caixa só.
/// </para>
/// <para>
/// <strong>Não há varredura recursiva.</strong> Subpasta não listada não é lida (decisão de
/// 2026-08-11); acrescente cada uma que interessa.
/// </para>
/// </remarks>
public sealed record AddCaptureSourceFolderCommand(
    Guid TenantId,
    Guid CaptureSourceId,
    string? FolderPath) : ITenantScopedCommand, IRequest<AddCaptureSourceFolderResponse>;

/// <param name="FolderId">A pasta criada. Nasce sem cursor: a primeira varredura dela lê tudo.</param>
public sealed record AddCaptureSourceFolderResponse(Guid Id, Guid FolderId);

public sealed class AddCaptureSourceFolderCommandHandler(
    ICaptureSourceRepository repository,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddCaptureSourceFolderCommand, AddCaptureSourceFolderResponse>
{
    public async Task<AddCaptureSourceFolderResponse> Handle(
        AddCaptureSourceFolderCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CaptureSourceId.From(request.CaptureSourceId);

        var source = await repository.GetAsync(tenantId, id, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        // Quem recusa pasta repetida (BLP.CPS16) e o teto de pastas (BLP.CPS19) é o agregado.
        var folder = source.AddFolder(request.FolderPath, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AddCaptureSourceFolderResponse(source.Id.Value, folder.Id.Value);
    }
}

public sealed class AddCaptureSourceFolderIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AddCaptureSourceFolderIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AddCaptureSourceFolderCommand, AddCaptureSourceFolderResponse>(
        mediator, requestManager, logger)
{
    protected override AddCaptureSourceFolderResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, Guid.Empty);
}

/// <summary>
/// Deixa de acompanhar uma pasta. Os itens já ingeridos dela permanecem.
/// </summary>
/// <remarks>
/// O agregado recusa remover a última (<c>BLP.CPS18</c>): fonte sem pasta nenhuma não varreria
/// nada e não avisaria — quem quer parar de varrer desativa a fonte.
/// </remarks>
public sealed record RemoveCaptureSourceFolderCommand(
    Guid TenantId,
    Guid CaptureSourceId,
    string? FolderPath) : ITenantScopedCommand, IRequest<RemoveCaptureSourceFolderResponse>;

public sealed record RemoveCaptureSourceFolderResponse(Guid Id);

public sealed class RemoveCaptureSourceFolderCommandHandler(
    ICaptureSourceRepository repository,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveCaptureSourceFolderCommand, RemoveCaptureSourceFolderResponse>
{
    public async Task<RemoveCaptureSourceFolderResponse> Handle(
        RemoveCaptureSourceFolderCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CaptureSourceId.From(request.CaptureSourceId);

        var source = await repository.GetAsync(tenantId, id, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        source.RemoveFolder(request.FolderPath, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RemoveCaptureSourceFolderResponse(source.Id.Value);
    }
}

public sealed class RemoveCaptureSourceFolderIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RemoveCaptureSourceFolderIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RemoveCaptureSourceFolderCommand, RemoveCaptureSourceFolderResponse>(
        mediator, requestManager, logger)
{
    protected override RemoveCaptureSourceFolderResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}

/// <summary>
/// Descarta o cursor de todas as pastas: a próxima varredura relê a caixa inteira.
/// </summary>
/// <remarks>
/// <para>
/// Existe por uma necessidade concreta e recorrente: <strong>o desfecho de um artefato depende do
/// cadastro que existia na hora em que ele passou</strong>. Sem <c>PayerProfile</c> não há senha
/// derivada; sem <c>Payee</c> nem <c>TrustedOrigin</c>, o que a cascata não reconhece é
/// descartado em vez de ficar em quarentena. Cadastrar depois não reavalia nada — e sem esta
/// operação a única saída era desconectar a fonte e reconectar, digitando a credencial de novo.
/// </para>
/// <para>
/// <strong>Reler não duplica.</strong> A ingestão é idempotente por
/// <c>(tenant, fonte, mensagem, anexo)</c>: o que já virou item continua o mesmo item. O que muda
/// é que o que foi <em>descartado</em> volta a ser avaliado, agora com o cadastro de hoje.
/// </para>
/// </remarks>
public sealed record RescanCaptureSourceCommand(Guid TenantId, Guid CaptureSourceId)
    : ITenantScopedCommand, IRequest<RescanCaptureSourceResponse>;

/// <param name="FoldersReset">Quantas pastas voltarão a ser lidas por inteiro.</param>
public sealed record RescanCaptureSourceResponse(Guid Id, int FoldersReset);

public sealed class RescanCaptureSourceCommandHandler(
    ICaptureSourceRepository repository,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RescanCaptureSourceCommand, RescanCaptureSourceResponse>
{
    public async Task<RescanCaptureSourceResponse> Handle(
        RescanCaptureSourceCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CaptureSourceId.From(request.CaptureSourceId);

        var source = await repository.GetAsync(tenantId, id, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        source.ResetAllCursors(clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RescanCaptureSourceResponse(source.Id.Value, source.Folders.Count);
    }
}

public sealed class RescanCaptureSourceIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RescanCaptureSourceIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RescanCaptureSourceCommand, RescanCaptureSourceResponse>(
        mediator, requestManager, logger)
{
    protected override RescanCaptureSourceResponse CreateResultForDuplicateRequest() => new(Guid.Empty, 0);
}
