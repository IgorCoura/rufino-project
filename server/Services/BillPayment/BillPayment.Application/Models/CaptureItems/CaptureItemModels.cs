namespace BillPayment.Application.Models.CaptureItems;

/// <summary>
/// Corpo da reprovação de um item da quarentena.
/// </summary>
/// <remarks>
/// <strong>Não carrega quem reprovou</strong> — a identidade vem do <c>sub</c> do token, como em
/// toda decisão do BC. Aceitar o autor pelo corpo permitiria reprovar em nome de outra pessoa, e
/// a trilha de auditoria da quarentena deixaria de valer.
/// </remarks>
public sealed class DismissCaptureItemModel
{
    /// <summary>
    /// Observação de quem reprovou. Opcional de propósito.
    /// </summary>
    /// <remarks>
    /// Exigir justificativa transforma uma decisão de dois segundos numa de trinta, e a fila
    /// deixa de ser esvaziável na prática — que é justamente o problema que a reprovação resolve.
    /// </remarks>
    public string? Note { get; set; }
}
