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
/// beneficiário só existem na <c>Bill</c>, a partir da 2.6. Mas <see cref="HasArtifact"/> e
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
    bool HasArtifact,
    string? SourceUrl,
    string? ContentHash,
    Guid? BillId,
    Guid? ClaimedBy,
    DateTime? ClaimedAt,

    /// <summary>Quantas vezes um worker já tentou processar este artefato.</summary>
    int ProcessingAttempts,

    /// <summary>A mensagem da última falha de processamento, quando houve.</summary>
    string? LastError,

    /// <summary>Quem hospeda o documento que a escada tentou — sem o caminho que o abre.</summary>
    string? LinkHost)
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

            // A chave saía inteira daqui, e não servia para nada do lado de fora: quem busca o
            // documento manda o id do item, e é o servidor que resolve a chave. Um booleano diz
            // a única coisa que a tela precisa — se há botão de "ver documento" — sem entregar
            // ponteiro de infraestrutura a quem só ia exibi-lo.
            exposes && item.HasStoredArtifact,
            // A URL segue um portão MAIS LARGO que os campos financeiros: ela é o que permite a
            // pessoa ir buscar o documento à mão quando a escada não alcançou o emissor. Escondê-la
            // na quarentena — que era o comportamento até 2026-08-26 — deixava o usuário sabendo
            // que existe uma cobrança e sem como chegar nela. `ForeignPayer` continua fechado.
            item.Status.ExposesSourceUrl ? item.SourceUrl : null,
            exposes ? item.ContentHash : null,
            exposes ? item.BillId?.Value : null,
            item.ClaimedBy?.Value,
            item.ClaimedAt,

            // Diagnóstico do processamento sai SEM o portão do ADR-008, ao contrário dos campos
            // acima: ele não descreve o documento nem o dinheiro, descreve o sistema. Sem isto,
            // um item em `Failed` chegaria à tela sem nada que explicasse por quê — que é o
            // estado em que a captura ficou por 1.709 tentativas antes de alguém abrir o log.
            item.ProcessingAttempts,
            item.LastError,

            // O HOST sai sem o portão do ADR-008; a URL inteira, não. A URL é credencial ao
            // portador — quem a tem, tem o boleto, que pode ser de outro pagador. O host só diz
            // QUEM emitiu, e é o dado que decide qual receita de link cadastrar. Sem ele, a
            // quarentena não responde "de onde veio isto que não conseguimos buscar".
            item.LinkHost);
    }
}

public sealed record CaptureItemPage(IReadOnlyList<CaptureItemDto> Items, string? NextCursor);
