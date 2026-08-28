namespace BillPayment.Application.Queries.CaptureItems;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class CaptureItemQueries(BillPaymentDbContext context, IAttachmentStorage storage)
    : ICaptureItemQueries
{
    public const int DEFAULT_LIMIT = 50;
    public const int MAX_LIMIT = 200;

    /// <summary>Nome usado quando o provedor não informou o do anexo.</summary>
    private const string DEFAULT_FILE_NAME = "documento";

    public async Task<CaptureItemPage> ListAsync(
        Guid tenantId,
        string? status,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? DEFAULT_LIMIT : limit, 1, MAX_LIMIT);
        var tenant = TenantId.From(tenantId);

        var query = context.CaptureItems.AsNoTracking().Where(i => i.TenantId == tenant);

        if (TryParseStatus(status, out var parsed))
            query = query.Where(i => i.Status == parsed);

        // Keyset ascendente por (CreatedAt, Id). O Id não é enfeite: a varredura carimba um
        // instante só para todos os itens que ingere, então CreatedAt empata às centenas — e um
        // cursor só com a data faria a página 2 voltar vazia, escondendo o resto da fila.
        if (CursorCodec.TryDecode(cursor, out var afterCreatedAt, out var afterId))
        {
            var afterItemId = CaptureItemId.From(afterId);

            query = query.Where(i =>
                i.CreatedAt > afterCreatedAt || (i.CreatedAt == afterCreatedAt && i.Id > afterItemId));
        }

        var rows = await query
            .OrderBy(i => i.CreatedAt)
            .ThenBy(i => i.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(rows[^1].CreatedAt, rows[^1].Id.Value)
            : null;

        return new CaptureItemPage(rows.ConvertAll(CaptureItemDto.From), nextCursor);
    }

    public async Task<CaptureItemDto?> GetAsync(
        Guid tenantId,
        Guid captureItemId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = CaptureItemId.From(captureItemId);

        var item = await context.CaptureItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TenantId == tenant && i.Id == id, cancellationToken);

        return item is null ? null : CaptureItemDto.From(item);
    }

    public async Task<ArtifactDownload?> GetArtifactAsync(
        Guid tenantId,
        Guid captureItemId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = CaptureItemId.From(captureItemId);

        var item = await context.CaptureItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TenantId == tenant && i.Id == id, cancellationToken);

        // As três negativas saem iguais de propósito — ver o contrato da interface.
        //
        // O portão é o MESMO da URL, e não o dos campos financeiros: a pessoa que anexa um boleto
        // à mão num item de quarentena precisa poder reabri-lo para conferir o que subiu. Com o
        // gate antigo, ela subia o arquivo e recebia 404 ao tentar vê-lo.
        if (item is null || !item.Status.ExposesSourceUrl || !item.HasStoredArtifact)
            return null;

        var artifact = await storage.OpenAsync(tenant, item.StorageKey!, cancellationToken);
        if (artifact is null)
            return null;

        return ArtifactDownload.From(artifact, item.ContentType, item.FileName ?? DEFAULT_FILE_NAME);
    }

    /// <summary>
    /// Status desconhecido devolve a lista inteira em vez de estourar: o filtro vem da query
    /// string e um valor inválido é erro do cliente, não motivo para 500.
    /// </summary>
    private static bool TryParseStatus(string? status, out CaptureItemStatus parsed)
    {
        parsed = default!;
        if (string.IsNullOrWhiteSpace(status))
            return false;

        parsed = Enumeration.GetAll<CaptureItemStatus>()
            .FirstOrDefault(s => string.Equals(s.Name, status.Trim(), StringComparison.OrdinalIgnoreCase))!;

        return parsed is not null;
    }
}
