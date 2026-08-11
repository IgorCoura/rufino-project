namespace BillPayment.Application.Queries.CaptureItems;

using BillPayment.Domain.CaptureItems;

/// <summary>
/// Um item capturado como o dono da fonte o vê — <strong>com o conteúdo filtrado pelo status</strong>.
/// </summary>
/// <remarks>
/// <para>
/// Esta é a materialização da regra do ADR-008: a quarentena tem dois níveis de visibilidade, e
/// eles são <em>de projeção</em>, não de UI. Quem decide é
/// <c>CaptureItemStatus.ExposesFinancialDetail</c>; este DTO só obedece — por isso a construção
/// passa obrigatoriamente por <see cref="From"/>, e não por um <c>new</c> espalhado pelas queries.
/// </para>
/// <para>
/// Os campos que o gate esconde não são "financeiros" no sentido literal ainda — valor e
/// beneficiário só existem na <c>Bill</c>, a partir da 2.6. Mas <see cref="StorageKey"/> e
/// <see cref="SourceUrl"/> <strong>levam ao documento</strong> de outro pagador, o que é o mesmo
/// vazamento por outro caminho. Quando os campos financeiros chegarem, entram atrás deste
/// mesmo gate.
/// </para>
/// </remarks>
public sealed record CaptureItemDto(
    Guid Id,
    Guid SourceId,
    string Sender,
    string? Subject,
    DateTime ReceivedAt,
    string Status,
    string? Reason,
    string? RoutingConfidence,
    string? ExtractionMethod,
    string? UnlockedBy,
    string? StorageKey,
    string? SourceUrl,
    string? ContentHash,
    Guid? BillId,
    Guid? ClaimedBy,
    DateTime? ClaimedAt)
{
    /// <summary>
    /// Projeta o item aplicando o nível de visibilidade do próprio status.
    /// </summary>
    /// <remarks>
    /// Um <c>ForeignPayer</c> devolve remetente, assunto, data e motivo — e nada que leve ao
    /// documento. O sistema <em>sabe</em> que aquele item não é deste usuário; entregar o
    /// ponteiro do arquivo ou o link da fatura seria vazamento gratuito.
    /// </remarks>
    public static CaptureItemDto From(CaptureItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var exposes = item.Status.ExposesFinancialDetail;

        return new CaptureItemDto(
            item.Id.Value,
            item.SourceId.Value,
            item.Sender,
            item.Subject,
            item.ReceivedAt,
            item.Status.Name,
            item.Reason,
            item.Routing?.Name,
            item.Extraction?.Name,

            // UnlockedBy diz QUAL campo do perfil abriu o PDF. Num item de outro pagador isso
            // revelaria que um documento nosso serviu de chave — sai só quando o item é do tenant.
            exposes ? item.UnlockedBy : null,

            exposes ? item.StorageKey : null,
            exposes ? item.SourceUrl : null,
            exposes ? item.ContentHash : null,
            exposes ? item.BillId?.Value : null,
            item.ClaimedBy?.Value,
            item.ClaimedAt);
    }
}

public sealed record CaptureItemPage(IReadOnlyList<CaptureItemDto> Items, string? NextCursor);
