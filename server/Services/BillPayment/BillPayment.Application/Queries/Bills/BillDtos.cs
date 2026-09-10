namespace BillPayment.Application.Queries.Bills;

/// <summary>
/// Detalhe para decidir. <strong>Continua sem a linha digitável e sem o payload Pix</strong> —
/// quem os tem, paga.
/// </summary>
/// <remarks>
/// <para>
/// A ordem dos campos espelha a ordem de leitura que o doc 03 exige da tela: identidade do
/// beneficiário primeiro, origem por último. Origem confiável nunca compensa beneficiário
/// errado, e a interface não deve sugerir o contrário.
/// </para>
/// <para>
/// <strong>Dia de calendário trafega como <c>DateOnly</c>; instante, como <c>DateTime</c> em
/// UTC.</strong> A distinção é o contrato, não estilo: até 2026-09-10 os dias saíam convertidos
/// para meia-noite UTC, e o cliente — que formata em hora local — mostrava o dia ANTERIOR em todo
/// fuso a oeste de Greenwich. O vencimento aparecia certo só na evidência do check, que é texto
/// montado no servidor. Os demais read models do BC (expectativas, ordens de pagamento, prévia de
/// agendamento) sempre usaram <c>DateOnly</c>; este era o destoante.
/// </para>
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
    DateOnly? DueDate,
    string? BankCode,
    DateOnly? MinimumScheduleDate,
    DateTime? LastConsultedAt,

    /// <summary>
    /// O instante em que o retrato da consulta deixa de sustentar aprovação e agendamento — já
    /// resolvido contra a política vigente (<c>Approval:MaxSnapshotAgeHours</c>). Nulo quando
    /// nunca houve consulta: não há prazo correndo sobre retrato que não existe.
    /// </summary>
    /// <remarks>
    /// <strong>Existe para a tela parar de replicar o prazo.</strong> O cliente carregava uma
    /// constante de 12 horas espelhando a configuração do servidor, e bastava mudar um dos dois
    /// para a tela passar a mentir — habilitando "Agendar…" num retrato que o servidor ia recusar
    /// com <c>BLP.BIL06</c>, ou o contrário. Vem resolvido, e não como número de horas, porque
    /// comparar dois instantes é a única conta que o cliente precisa fazer.
    /// </remarks>
    DateTime? SnapshotExpiresAt,

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
    DateOnly? ScheduledFor,
    BillOriginDto Origin,
    DateTime CreatedAt,

    /// <summary>
    /// A trilha completa, do mais antigo para o mais recente: o que foi feito, quando e por quem.
    /// </summary>
    /// <remarks>
    /// Vem inteira porque é o que a tela expande de uma vez, e porque a decisão vigente
    /// (<see cref="Approval"/>) responde "vale?" enquanto esta responde "o que houve?" — as duas
    /// perguntas convivem no mesmo detalhe.
    /// </remarks>
    IReadOnlyList<BillHistoryEntryDto> History);

/// <param name="ActorName">
/// O nome de quem agiu, congelado no instante da ação. "Sistema" quando não partiu de uma pessoa.
/// </param>
/// <param name="Origin">
/// De ONDE partiu — <c>User</c>, <c>Provider</c> ou <c>System</c>. É o que permite a tela dizer
/// "cancelado NO PROVEDOR" em vez de atribuir a ação ao sistema.
/// </param>
public sealed record BillHistoryEntryDto(
    string Action,
    string Origin,
    DateTime OccurredAt,
    Guid? ActorUserId,
    string ActorName,
    string? FromStatus,
    string ToStatus,
    string? Note);

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
    DateOnly? DueDate,
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
    DateOnly? DueDate,
    DateOnly? MinimumScheduleDate,
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
    DateOnly? DueDate,

    /// <summary>
    /// Quando o QR deixa de valer. <strong>É instante, não dia</strong> — o Pix dinâmico expira na
    /// hora, e truncar para dia aqui esconderia a hora que decide se ainda dá para pagar.
    /// </summary>
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
    DateOnly? DueDate,
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
    string ReadingStatus,

    /// <summary>
    /// A data de pagamento — pedida na aprovação, efetiva depois do agendamento (fase 3).
    /// Na lista, porque um boleto Agendado sem data visível obriga a abrir o detalhe.
    /// </summary>
    DateOnly? ScheduledFor = null);

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
