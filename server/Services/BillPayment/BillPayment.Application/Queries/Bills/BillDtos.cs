namespace BillPayment.Application.Queries.Bills;

/// <summary>
/// Detalhe para decidir. <strong>Continua sem a linha digitável e sem o payload Pix</strong> —
/// quem os tem, paga.
/// </summary>
/// <remarks>
/// A ordem dos campos espelha a ordem de leitura que o doc 03 exige da tela: identidade do
/// beneficiário primeiro, origem por último. Origem confiável nunca compensa beneficiário
/// errado, e a interface não deve sugerir o contrário.
/// </remarks>
public sealed record BillDetailDto(
    Guid Id,
    string Status,
    string Kind,
    string Rail,
    BillPartyDto? Beneficiary,
    decimal? Amount,
    decimal? OriginalAmount,
    DateTime? DueDate,
    string? BankCode,
    DateTime? MinimumScheduleDate,
    DateTime? LastConsultedAt,
    IReadOnlyList<BillCheckDto> Checks,
    BillApprovalDto? Approval,
    DateTime? ScheduledFor,
    BillOriginDto Origin,
    DateTime CreatedAt);

public sealed record BillPartyDto(string? Name, string? TradingName, string? TaxId);

/// <summary>
/// <c>ReasonCode</c> é o contrato de tradução da UI; <c>Evidence</c> é o texto que explica a
/// decisão ao humano. Os dois viajam porque servem a propósitos diferentes.
/// </summary>
public sealed record BillCheckDto(
    string Type,
    string Outcome,
    string Severity,
    string? ReasonCode,
    string? Evidence,
    bool IsBlockingFailure,
    DateTime EvaluatedAt);

public sealed record BillApprovalDto(Guid DecidedBy, string Decision, DateTime DecidedAt, string? Note);

/// <summary>
/// <strong>Não expõe a linha digitável nem o payload Pix.</strong> São instrumentos de
/// pagamento: quem os tem, paga. A tela precisa do valor, do vencimento e do banco para
/// decidir — não do meio de pagar.
/// </summary>
public sealed record BillDto(
    Guid Id,
    string Status,
    string Kind,
    string Rail,
    decimal? Amount,
    DateTime? DueDate,
    string? BankCode,
    BillOriginDto Origin,
    DateTime CreatedAt);

public sealed record BillOriginDto(
    string SourceKind,
    Guid? SourceId,
    string? SenderAddress,
    DateTime ReceivedAt);

public sealed record BillPage(IReadOnlyList<BillDto> Items, string? NextCursor);
