namespace BillPayment.Application.Models.Bills;

using System.Text.Json.Serialization;
using BillPayment.Application.Bills.Commands;

/// <summary>
/// Modelos HTTP existem porque o <c>tenantId</c> vem da rota, não do corpo — mandar o
/// tenant no body abriria caminho para divergir do path e virar IDOR.
/// </summary>
/// <remarks>
/// <c>ReceivedAt</c> é <c>[JsonRequired]</c> porque a omissão viraria <c>default</c>
/// silenciosamente e gravaria o ano 1 como data de recebimento — a evidência de origem
/// existe justamente para ser confiável na auditoria.
/// </remarks>
public sealed record ImportBillModel(
    string? DigitableLine,
    string? PixPayload,
    string SourceKind,
    [property: JsonRequired] DateTime ReceivedAt,
    Guid? SourceId,
    string? SenderAddress,
    string? ExternalMessageId,
    string? ContentHash,
    string? StorageKey)
{
    public ImportBillCommand ToCommand(Guid tenantId)
        => new(
            tenantId,
            DigitableLine,
            PixPayload,
            SourceKind,
            ReceivedAt,
            SourceId,
            SenderAddress,
            ExternalMessageId,
            ContentHash,
            StorageKey);
}

/// <summary>
/// A data de pagamento é escolha do aprovador, e por isso vem no corpo. Quem decide é resolvido
/// do token (ou, nesta fase, do header) — nunca do body, para não ser possível aprovar em nome
/// de outra pessoa.
/// </summary>
public sealed record ApproveBillModel([property: JsonRequired] DateOnly ScheduleFor, string? Note)
{
    public ApproveBillCommand ToCommand(Guid tenantId, Guid billId, Guid decidedBy)
        => new(tenantId, billId, decidedBy, ScheduleFor, Note);
}

public sealed record BillDecisionModel([property: JsonRequired] string Reason)
{
    public DenyBillCommand ToDenyCommand(Guid tenantId, Guid billId, Guid decidedBy)
        => new(tenantId, billId, decidedBy, Reason);

    public CancelBillCommand ToCancelCommand(Guid tenantId, Guid billId, Guid decidedBy)
        => new(tenantId, billId, decidedBy, Reason);
}
