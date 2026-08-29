namespace BillPayment.Application.Queries.CapturedMessages;

using System.Text;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Extraction;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Leitura do livro-caixa da captura.
/// </summary>
public interface ICapturedMessageQueries
{
    /// <summary>
    /// Lista os e-mails lidos, do mais recente para o mais antigo.
    /// </summary>
    /// <param name="outcome">
    /// Nome de um <c>ArtifactOutcome</c>. Casa quando <strong>qualquer</strong> anexo tem aquele
    /// desfecho — filtrar pelo dominante esconderia o e-mail que trouxe um boleto e um recibo.
    /// Nulo ou desconhecido devolve tudo, como os demais filtros do BC.
    /// </param>
    /// <param name="search">
    /// Trecho do remetente ou do assunto. Sem acento e sem caixa — quem busca digita "enel", não
    /// "faturas@enel.com.br".
    /// </param>
    Task<CapturedMessagePage> ListAsync(
        Guid tenantId,
        string? outcome,
        Guid? sourceId,
        DateTime? from,
        DateTime? to,
        string? search,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<CapturedMessageDto?> GetAsync(
        Guid tenantId,
        Guid capturedMessageId,
        CancellationToken cancellationToken = default);

    /// <summary>Quando a caixa foi lida pela última vez — o cabeçalho da tela.</summary>
    Task<CaptureSyncStatusDto> GetSyncStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// O e-mail inteiro — cabeçalho e corpo renderizável. Nulo quando a mensagem não existe ou o
    /// corpo não está guardado nem alcançável no provedor.
    /// </summary>
    Task<CapturedMessageBodyDto?> GetBodyAsync(
        Guid tenantId,
        Guid capturedMessageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// O e-mail que trouxe um boleto, resolvido pela origem da <c>Bill</c>. Nulo para boleto que
    /// não veio de caixa de e-mail.
    /// </summary>
    Task<CapturedMessageBodyDto?> GetBodyForBillAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// O e-mail que trouxe um item da quarentena. Nulo para anexo manual — não há e-mail por
    /// trás — e para mensagem que a retenção já purgou e o provedor não devolve mais.
    /// </summary>
    Task<CapturedMessageBodyDto?> GetBodyForCaptureItemAsync(
        Guid tenantId,
        Guid captureItemId,
        CancellationToken cancellationToken = default);
}

internal sealed class CapturedMessageQueries(
    BillPaymentDbContext context,
    IAttachmentStorage storage,
    IMailboxReader mailboxReader) : ICapturedMessageQueries
{
    public const int DEFAULT_LIMIT = 50;
    public const int MAX_LIMIT = 200;

    public async Task<CapturedMessagePage> ListAsync(
        Guid tenantId,
        string? outcome,
        Guid? sourceId,
        DateTime? from,
        DateTime? to,
        string? search,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? DEFAULT_LIMIT : limit, 1, MAX_LIMIT);
        var tenant = TenantId.From(tenantId);

        var query = context.CapturedMessages
            .AsNoTracking()
            .Include(m => m.Artifacts)
            .Where(m => m.TenantId == tenant);

        if (sourceId is { } source)
            query = query.Where(m => m.SourceId == Domain.CaptureSources.CaptureSourceId.From(source));

        if (from is { } start)
            query = query.Where(m => m.ReceivedAt >= start);

        if (to is { } end)
            query = query.Where(m => m.ReceivedAt <= end);

        if (TryParseOutcome(outcome, out var parsed))
        {
            // `NothingToProcess` não existe em linha de anexo nenhuma — ele É a ausência delas.
            // Casá-lo pelo `Any` devolveria lista vazia justamente para o filtro que o usuário
            // usa quando quer ver o que o sistema decidiu ignorar.
            query = parsed == ArtifactOutcome.NothingToProcess
                ? query.Where(m => !m.Artifacts.Any())
                : query.Where(m => m.Artifacts.Any(a => a.Outcome == parsed));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(m =>
                EF.Functions.ILike(m.Sender, pattern)
                || (m.Subject != null && EF.Functions.ILike(m.Subject, pattern)));
        }

        // Keyset descendente por (ReceivedAt, Id) — o mais recente primeiro, que é a ordem em que
        // alguém procura "o e-mail que acabei de mandar". O desempate acompanha a direção da
        // chave; cruzar as direções faz ORDER BY e WHERE discordarem sobre quem já foi visto.
        if (CursorCodec.TryDecode(cursor, out var beforeReceivedAt, out var beforeId))
        {
            var beforeMessageId = CapturedMessageId.From(beforeId);

            query = query.Where(m =>
                m.ReceivedAt < beforeReceivedAt
                || (m.ReceivedAt == beforeReceivedAt && m.Id < beforeMessageId));
        }

        var rows = await query
            .OrderByDescending(m => m.ReceivedAt)
            .ThenByDescending(m => m.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(rows[^1].ReceivedAt, rows[^1].Id.Value)
            : null;

        return new CapturedMessagePage(rows.ConvertAll(ToDto), nextCursor);
    }

    public async Task<CapturedMessageDto?> GetAsync(
        Guid tenantId,
        Guid capturedMessageId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = CapturedMessageId.From(capturedMessageId);

        var message = await context.CapturedMessages
            .AsNoTracking()
            .Include(m => m.Artifacts)
            .FirstOrDefaultAsync(m => m.TenantId == tenant && m.Id == id, cancellationToken);

        return message is null ? null : ToDto(message);
    }

    public async Task<CaptureSyncStatusDto> GetSyncStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);

        var sources = await context.CaptureSources
            .AsNoTracking()
            .Where(s => s.TenantId == tenant)
            .Select(s => s.LastSyncAt)
            .ToListAsync(cancellationToken);

        // A mais recente entre as fontes: a tela mostra um número só, e o que interessa a quem
        // acabou de mandar um e-mail é se ALGUMA varredura já rodou depois disso.
        return new CaptureSyncStatusDto(sources.Max(), sources.Count);
    }

    /// <summary>
    /// O desfecho que a linha mostra sem expandir.
    /// </summary>
    /// <remarks>
    /// A ordem é de gravidade decrescente, não de contagem: um e-mail com um boleto e um recibo
    /// descartado é "virou boleto", e um com uma falha de download entre três descartes precisa
    /// mostrar a falha — é ela que pede ação.
    /// </remarks>
    private static ArtifactOutcome Dominant(CapturedMessage message)
    {
        var artifacts = message.Artifacts;

        // Sem anexo não há espera — há nada a fazer. Cair no fallback aqui fazia a tela dizer
        // "Na fila" para sempre a respeito de propaganda e notificação, que é o defeito relatado
        // em 2026-08-26: 23 das 39 mensagens da caixa real, nenhuma delas em fila alguma.
        if (artifacts.Count == 0)
            return ArtifactOutcome.NothingToProcess;

        foreach (var candidate in Priority)
        {
            foreach (var artifact in artifacts)
            {
                if (artifact.Outcome == candidate)
                    return candidate;
            }
        }

        // Inalcançável enquanto `Priority` cobrir o catálogo — e é `PriorityCoversEveryOutcome`,
        // na suíte unitária, que garante isso. Sem aquele teste, um desfecho novo esquecido aqui
        // volta a ser exibido como "Na fila", sem quebrar compilação nem teste.
        return ArtifactOutcome.Pending;
    }

    /// <summary>
    /// Ordem de gravidade decrescente — <strong>tem de conter o catálogo inteiro</strong>.
    /// </summary>
    /// <remarks>
    /// Desfecho ausente daqui não aparece na tela: ele escorre pelo laço e vira <c>Pending</c>,
    /// que o usuário lê como "Na fila". Aconteceu com <see cref="ArtifactOutcome.ProcessingFailed"/>,
    /// acrescentado ao catálogo e esquecido nesta lista no mesmo dia — o anexo que desistiu de
    /// processar aparecia como se ainda estivesse esperando.
    /// </remarks>
    private static readonly ArtifactOutcome[] Priority =
    [
        ArtifactOutcome.Promoted,
        ArtifactOutcome.Unrouted,
        ArtifactOutcome.ProcessingFailed,
        ArtifactOutcome.DownloadFailed,
        ArtifactOutcome.Locked,
        ArtifactOutcome.Quarantined,
        ArtifactOutcome.ForeignPayer,
        ArtifactOutcome.Pending,
        ArtifactOutcome.Dismissed,
        ArtifactOutcome.Discarded,
        ArtifactOutcome.NothingToProcess,
    ];

    private static bool TryParseOutcome(string? outcome, out ArtifactOutcome parsed)
    {
        parsed = default!;
        if (string.IsNullOrWhiteSpace(outcome))
            return false;

        parsed = Enumeration.GetAll<ArtifactOutcome>()
            .FirstOrDefault(o => string.Equals(o.Name, outcome.Trim(), StringComparison.OrdinalIgnoreCase))!;

        return parsed is not null;
    }

    private static CapturedMessageDto ToDto(CapturedMessage message)
        => new(
            message.Id.Value,
            message.SourceId.Value,
            message.Sender,
            message.Subject,
            message.ReceivedAt,
            message.FirstSeenAt,
            message.ProcessedAt,
            Dominant(message).Name,
            message.ArtifactCount,
            message.CanBeRecaptured,
            [.. message.Artifacts
                .OrderBy(a => a.FileName)
                .Select(a => new MessageArtifactDto(
                    a.FileName,
                    a.ContentType,
                    a.Outcome.Name,
                    a.Reason,
                    a.CaptureItemId?.Value,
                    a.BillId?.Value,
                    a.DecidedAt))]);
    public async Task<CapturedMessageBodyDto?> GetBodyAsync(
        Guid tenantId,
        Guid capturedMessageId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = CapturedMessageId.From(capturedMessageId);

        var message = await context.CapturedMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == tenant && m.Id == id, cancellationToken);

        return message is null
            ? null
            : await ComposeBodyAsync(tenant, message, cancellationToken);
    }

    public async Task<CapturedMessageBodyDto?> GetBodyForBillAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = BillId.From(billId);

        var bill = await context.Bills
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenant && b.Id == id, cancellationToken);

        // Importação manual não tem e-mail por trás — ausência é estado normal, não erro.
        if (bill?.Origin.SourceId is not { } sourceId || string.IsNullOrEmpty(bill.Origin.ExternalMessageId))
            return null;

        var source = CaptureSourceId.From(sourceId);
        var externalMessageId = bill.Origin.ExternalMessageId;

        var message = await context.CapturedMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenant && m.SourceId == source && m.ExternalMessageId == externalMessageId,
                cancellationToken);

        return message is null
            ? null
            : await ComposeBodyAsync(tenant, message, cancellationToken);
    }

    public async Task<CapturedMessageBodyDto?> GetBodyForCaptureItemAsync(
        Guid tenantId,
        Guid captureItemId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = CaptureItemId.From(captureItemId);

        var item = await context.CaptureItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TenantId == tenant && i.Id == id, cancellationToken);

        // Anexo manual não tem e-mail por trás — ausência é estado normal, não erro.
        if (item is null || item.ManuallySupplied)
            return null;

        var sourceId = item.SourceId;
        var externalMessageId = item.ExternalMessageId;

        var message = await context.CapturedMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenant && m.SourceId == sourceId && m.ExternalMessageId == externalMessageId,
                cancellationToken);

        return message is null
            ? null
            : await ComposeBodyAsync(tenant, message, cancellationToken);
    }

    /// <summary>
    /// O corpo guardado no balde; sem ele, o plano B rebusca no provedor — é o caminho das
    /// mensagens registradas antes de o corpo ser retido na sincronização.
    /// </summary>
    private async Task<CapturedMessageBodyDto?> ComposeBodyAsync(
        TenantId tenant,
        CapturedMessage message,
        CancellationToken cancellationToken)
    {
        if (message.HasStoredBody)
        {
            var stored = await storage.RetrieveAsync(tenant, message.BodyStorageKey!, cancellationToken);
            return ToBodyDto(message, message.BodyContentType ?? "text/html", stored);
        }

        var source = await context.CaptureSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenant && s.Id == message.SourceId, cancellationToken);

        if (source?.Credential is null)
            return null;

        var body = await mailboxReader.DownloadArtifactAsync(
            source.Address,
            source.Credential,
            message.ExternalMessageId,
            IMailboxReader.BODY_ARTIFACT_KEY,
            cancellationToken);

        return body is { IsEmpty: false }
            ? ToBodyDto(message, "text/html", body.Value)
            : null;
    }

    // HTML sai sanitizado — o remetente é a internet, e a API não pode entregar conteúdo ativo
    // para a tela do tenant (auditoria 2026-08-28). O que está GUARDADO continua sendo o original.
    private static CapturedMessageBodyDto ToBodyDto(
        CapturedMessage message,
        string contentType,
        ReadOnlyMemory<byte> content)
    {
        var text = Encoding.UTF8.GetString(content.Span);

        if (contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
            text = EmailBodySanitizer.Sanitize(text);

        return new(
            message.Id.Value,
            message.Sender,
            message.Subject,
            message.ReceivedAt,
            contentType,
            text);
    }
}
