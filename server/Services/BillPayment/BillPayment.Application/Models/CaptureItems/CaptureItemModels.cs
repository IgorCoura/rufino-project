namespace BillPayment.Application.Models.CaptureItems;

using BillPayment.Application.CaptureItems.Commands;

/// <summary>
/// Corpo da reivindicação de um item da quarentena. Opcional: sem corpo, reivindica sem lembrar.
/// </summary>
/// <remarks>
/// Como na reprovação, <strong>quem reivindica vem do token</strong>, nunca do corpo.
/// </remarks>
public sealed class ClaimCaptureItemModel
{
    /// <summary>
    /// "Lembrar desta conta" (ADR-026): o número que deve rotear os próximos boletos deste emissor.
    /// Vazio quando a pessoa desmarcou.
    /// </summary>
    public string? RememberAccountReference { get; set; }

    public ClaimCaptureItemCommand ToCommand(Guid tenantId, Guid captureItemId, Guid userId)
        => new(tenantId, captureItemId, userId, RememberAccountReference);
}

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
