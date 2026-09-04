namespace BillPayment.Application.CaptureItems.Commands;

using System.Security.Cryptography;
using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Uma pessoa buscou o boleto à mão e o anexou ao item que o sistema não conseguiu resolver.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Fecha o caminho que a escada de link não alcança.</strong> Emissor cuja página exige
/// login, cujo documento é movido por JavaScript, ou que simplesmente não tem receita cadastrada
/// — todos terminam em quarentena com a URL à vista. Daqui em diante a pessoa abre o link, baixa
/// o PDF e o devolve ao sistema, que a partir daí faz o de sempre: cascata, roteamento,
/// verificação e aprovação.
/// </para>
/// <para>
/// <strong>O item volta para <c>Received</c> em vez de ser processado aqui.</strong> É a mesma
/// razão do <c>Reopen</c>: um segundo caminho de processamento seria um segundo lugar para as
/// regras envelhecerem. O worker de sempre pega o item no ciclo seguinte.
/// </para>
/// </remarks>
/// <param name="Content">Os bytes do arquivo. Já lidos — o teto de tamanho é conferido na borda.</param>
public sealed record AttachCaptureItemArtifactCommand(
    Guid TenantId,
    Guid CaptureItemId,
    ReadOnlyMemory<byte> Content,
    string? ContentType,
    string? FileName) : ITenantScopedCommand, IRequest<AttachCaptureItemArtifactResponse>, ISensitiveCommand;

public sealed record AttachCaptureItemArtifactResponse(Guid Id, string PreviousStatus, int Bytes);

public sealed class AttachCaptureItemArtifactCommandHandler(
    ICaptureItemRepository items,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<AttachCaptureItemArtifactCommandHandler> logger)
    : IRequestHandler<AttachCaptureItemArtifactCommand, AttachCaptureItemArtifactResponse>
{
    public async Task<AttachCaptureItemArtifactResponse> Handle(
        AttachCaptureItemArtifactCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var itemId = CaptureItemId.From(request.CaptureItemId);

        var item = await items.GetAsync(tenantId, itemId, cancellationToken)
            ?? throw CaptureItemErrors.NotFound(request.CaptureItemId);

        // O tipo tem de ser um que a cascata saiba abrir. Recusar aqui, ANTES de gravar, é o que
        // impede o balde de acumular arquivo que nunca poderá ser lido.
        if (!DocumentPayload.IsSupported(request.ContentType))
            throw CaptureItemErrors.UnsupportedArtifact(request.ContentType ?? "desconhecido");

        if (request.Content.IsEmpty)
            throw CaptureItemErrors.ArtifactRequired();

        var previous = item.Status.Name;
        var now = clock.GetUtcNow().UtcDateTime;

        // A chave do artefato distingue os irmãos da mesma mensagem e continua sendo a mesma: o
        // documento anexado ocupa o lugar do que não veio, não cria um item novo.
        var storageKey = await storage.StoreAsync(
            tenantId, item.ArtifactKey, request.ContentType!, request.Content, cancellationToken);

        var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(request.Content.Span));

        // Quem recusa anexar ao que não aceita intervenção humana é o agregado: item já promovido
        // a boleto, ou já atribuído a outro pagador, não recebe documento por este caminho.
        item.AttachManualArtifact(hash, storageKey, request.ContentType, request.FileName, now);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Capture item {ItemId} received a manual artifact and went back to the queue (was {PreviousStatus}).",
                item.Id.Value,
                previous);
        }

        return new AttachCaptureItemArtifactResponse(item.Id.Value, previous, request.Content.Length);
    }
}

public sealed class AttachCaptureItemArtifactIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AttachCaptureItemArtifactIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AttachCaptureItemArtifactCommand, AttachCaptureItemArtifactResponse>(
        mediator, requestManager, logger)
{
    protected override AttachCaptureItemArtifactResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, 0);
}
