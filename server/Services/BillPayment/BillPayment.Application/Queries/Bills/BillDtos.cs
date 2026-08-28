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
    string? RiskLevel,
    BillPartyDto? Beneficiary,
    decimal? Amount,
    decimal? OriginalAmount,
    DateTime? DueDate,
    string? BankCode,
    DateTime? MinimumScheduleDate,
    DateTime? LastConsultedAt,
    BillReadingDto? Reading,

    /// <summary>
    /// Em que pé está a leitura por IA — <c>NotApplicable</c>, <c>Queued</c>, <c>Done</c> ou
    /// <c>Unavailable</c>. Espelha o campo homônimo de <see cref="BillDto"/>.
    /// </summary>
    /// <remarks>
    /// <strong>Sem ele a tela de DECISÃO era a única que não sabia.</strong> O campo existia só
    /// na listagem, então o detalhe — onde a pessoa aprova — não tinha como distinguir "este
    /// documento não tem o que ler" de "a análise ainda não chegou", e o check 13 aparecia como
    /// "Não se aplica / Sem leitura por IA" nos dois casos. É o lugar em que a informação mais
    /// importa, e era o único em que ela não chegava.
    /// </remarks>
    string ReadingStatus,

    BillLookupsDto Lookups,
    IReadOnlyList<BillCheckDto> Checks,
    BillApprovalDto? Approval,
    DateTime? ScheduledFor,
    BillOriginDto Origin,
    DateTime CreatedAt);

public sealed record BillPartyDto(string? Name, string? TradingName, string? TaxId);

/// <summary>
/// O retrato da leitura por IA do documento e do corpo do e-mail — enriquecimento e conferência,
/// nunca decisão de pagamento (ADR-011). Campos nulos são ausência honesta: o extrator não leu.
/// </summary>
public sealed record BillReadingDto(
    string? PayerName,
    string? PayerTaxId,
    string? PayeeName,
    string? PayeeTaxId,
    string? AccountReference,
    decimal? Amount,
    DateTime? DueDate,
    string? BillingPeriod,
    int? CompetenceYear,
    int? CompetenceMonth,
    string? Description,
    DateTime ReadAt);

/// <summary>
/// Os retratos das consultas oficiais, expostos por inteiro (decisão de 2026-08-27): é o que dá
/// ao aprovador toda a informação que o provedor devolveu, para uma decisão consciente.
/// </summary>
public sealed record BillLookupsDto(
    BankSlipLookupDto? BankSlip,
    PixLookupDto? Pix);

/// <summary>O retrato do <c>bill/simulate</c> — a fonte autoritativa do trilho de boleto.</summary>
public sealed record BankSlipLookupDto(
    BillPartyDto? Beneficiary,
    string? BankCode,
    decimal? Amount,
    decimal? OriginalAmount,
    decimal? Fee,
    bool AllowChangeValue,
    bool IsOverdue,
    DateTime? DueDate,
    DateTime? MinimumScheduleDate,
    DateTime ConsultedAt);

/// <summary>O retrato do <c>pix/qrCodes/decode</c> — a fonte autoritativa do trilho Pix.</summary>
public sealed record PixLookupDto(
    BillPartyDto? Receiver,
    string? ReceiverIspb,
    string? ReceiverIspbName,
    bool IsDynamic,
    bool CanBePaid,
    decimal? Amount,
    decimal? TotalAmount,
    decimal? Interest,
    decimal? Fine,
    decimal? Discount,
    DateTime? DueDate,
    DateTime? ExpiresAt,
    DateTime ConsultedAt);

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
    string? RiskLevel,
    BillPartyDto? Beneficiary,
    decimal? Amount,
    DateTime? DueDate,
    string? BankCode,
    BillOriginDto Origin,
    DateTime CreatedAt,

    /// <summary>
    /// Em que pé está a leitura por IA — <c>NotApplicable</c>, <c>Queued</c>, <c>Done</c> ou
    /// <c>Unavailable</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Existe para a tela parar de mentir.</strong> Sem ele, um boleto sem retrato é
    /// indistinguível de um boleto cujo documento não tem o que ler — e metade deles estava sem
    /// retrato por falha do provedor, não por ausência de conteúdo.
    /// </remarks>
    string ReadingStatus);

/// <param name="HasArtifact">
/// Se existe documento original para servir. <strong>Booleano, e não a chave</strong>: o download
/// recebe o id do boleto e resolve a chave no servidor, então o ponteiro não tem o que fazer do
/// lado de fora. Falso é estado normal — importação manual nasce só com os dígitos.
/// </param>
public sealed record BillOriginDto(
    string SourceKind,
    Guid? SourceId,
    string? SenderAddress,
    DateTime ReceivedAt,
    bool HasArtifact);

public sealed record BillPage(IReadOnlyList<BillDto> Items, string? NextCursor);
