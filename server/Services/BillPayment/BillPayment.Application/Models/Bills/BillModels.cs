namespace BillPayment.Application.Models.Bills;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BillPayment.Application.Bills.Commands;

/// <summary>
/// Modelos HTTP existem porque o <c>tenantId</c> vem da rota, não do corpo — mandar o
/// tenant no body abriria caminho para divergir do path e virar IDOR.
/// </summary>
/// <remarks>
/// <para>
/// <c>ReceivedAt</c> é <c>[JsonRequired]</c> porque a omissão viraria <c>default</c>
/// silenciosamente e gravaria o ano 1 como data de recebimento — a evidência de origem
/// existe justamente para ser confiável na auditoria.
/// </para>
/// <para>
/// <strong>Não há <c>ContentHash</c> nem <c>StorageKey</c> aqui</strong> (saíram em 2026-08-28).
/// Os dois descrevem um arquivo que o sistema guardou; aceitá-los do corpo deixava quem tem
/// <c>bill:import</c> apontar a "evidência" do boleto para qualquer objeto do balde do tenant —
/// servido depois por <c>GET /bills/{id}/artifact</c> e enfileirado para a IA. Quem quer o
/// arquivo junto usa a forma <c>multipart/form-data</c>, onde o handler grava e carimba.
/// <c>SourceId</c> continua, mas é validado contra as fontes do próprio tenant.
/// </para>
/// </remarks>
public sealed record ImportBillModel(
    string? DigitableLine,
    string? PixPayload,
    string SourceKind,
    [property: JsonRequired] DateTime ReceivedAt,
    Guid? SourceId,
    string? SenderAddress,
    string? ExternalMessageId)
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
            ExternalMessageId);
}

/// <summary>
/// A mesma importação, quando ela carrega o arquivo do boleto e chega como
/// <c>multipart/form-data</c>.
/// </summary>
/// <remarks>
/// <para>
/// É um modelo separado porque o binder de formulário não é o de JSON: <c>[JsonRequired]</c>
/// não vale aqui, e um <c>DateTime</c> não-anulável ausente viraria <c>default</c> — o ano 1
/// gravado como data de recebimento, em silêncio. <c>DateOnly?</c> mais <c>[Required]</c> é o
/// que faz o binder recusar a omissão em vez de inventar um valor.
/// </para>
/// <para>
/// <strong>O arquivo não vem por aqui.</strong> <c>IFormFile</c> é tipo do ASP.NET Core, e a
/// Application não conhece a borda HTTP: o controller lê os bytes e os entrega ao
/// <c>ToCommand</c>.
/// </para>
/// <para>
/// <strong><c>[Required]</c> fica no PARÂMETRO, não na propriedade.</strong> Num record posicional
/// o MVC recusa o modelo inteiro — com <c>InvalidOperationException</c>, não com 400 — quando a
/// validação é declarada em <c>[property: …]</c>, porque é o construtor primário que ele usa para
/// vincular. Escrito do jeito errado, toda importação com arquivo respondia 400 genérico.
/// </para>
/// </remarks>
public sealed record ImportBillFormModel(
    string? DigitableLine,
    string? PixPayload,
    [Required] string? SourceKind,
    [Required] DateTime? ReceivedAt,
    Guid? SourceId,
    string? SenderAddress,
    string? ExternalMessageId)
{
    public ImportBillCommand ToCommand(
        Guid tenantId,
        ReadOnlyMemory<byte> document,
        string? documentContentType,
        string? documentFileName)
        => new(
            tenantId,
            DigitableLine,
            PixPayload,
            SourceKind!,
            ReceivedAt!.Value,
            SourceId,
            SenderAddress,
            ExternalMessageId,

            // Hash e chave de armazenamento do arquivo são produzidos pelo handler, que é quem
            // grava — o Command nem tem campo para eles vindos de fora.
            document,
            documentContentType,
            documentFileName);
}

/// <summary>
/// Quem decide é resolvido do token — nunca do body, para não ser possível aprovar em nome de
/// outra pessoa.
/// </summary>
/// <param name="ScheduleFor">
/// <strong>Opcional desde o ADR-018.</strong> Ausente, o boleto só é aprovado e fica esperando
/// alguém agendar; presente, aprova e agenda na mesma transação — é o "Aprovar e agendar" da
/// tela, e exige também a alçada de agendamento.
/// </param>
/// <param name="AcknowledgeRisk">
/// ADR-015: <c>true</c> declara que o aprovador viu a classificação Perigo e decide mesmo assim.
/// </param>
public sealed record ApproveBillModel(
    DateOnly? ScheduleFor,
    string? Note,
    bool AcknowledgeRisk = false,
    bool AcknowledgeImmediateExecution = false)
{
    // Nem a alçada nem o nome vêm do body — os dois são resolvidos pelo controller (escopos UMA
    // e claims do token), pelo mesmo motivo que o UserId: quem chega à API não escolhe a própria
    // alçada nem assina a trilha com o nome de outro.
    public ApproveBillCommand ToCommand(
        Guid tenantId, Guid billId, Guid decidedBy, string riskClearance, string? actorName)
        => new(
            tenantId, billId, decidedBy, ScheduleFor, Note, riskClearance, actorName,
            AcknowledgeRisk, AcknowledgeImmediateExecution);
}

/// <summary>A data de pagamento é escolha de quem agenda, e por isso vem no corpo.</summary>
public sealed record ScheduleBillModel(
    [property: JsonRequired] DateOnly ScheduleFor,
    bool AcknowledgeImmediateExecution = false)
{
    public ScheduleBillCommand ToCommand(Guid tenantId, Guid billId, Guid decidedBy, string? actorName)
        => new(tenantId, billId, decidedBy, ScheduleFor, actorName, AcknowledgeImmediateExecution);
}

public sealed record BillDecisionModel([property: JsonRequired] string Reason)
{
    public DenyBillCommand ToDenyCommand(Guid tenantId, Guid billId, Guid decidedBy, string? actorName)
        => new(tenantId, billId, decidedBy, Reason, actorName);

    public CancelBillCommand ToCancelCommand(Guid tenantId, Guid billId, Guid decidedBy, string? actorName)
        => new(tenantId, billId, decidedBy, Reason, actorName);

    public UndoBillDecisionCommand ToUndoCommand(Guid tenantId, Guid billId, Guid decidedBy, string? actorName)
        => new(tenantId, billId, decidedBy, Reason, actorName);
}
