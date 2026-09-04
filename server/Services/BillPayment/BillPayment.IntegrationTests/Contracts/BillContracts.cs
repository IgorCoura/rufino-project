namespace BillPayment.IntegrationTests.Contracts;

// DTOs DUPLICADOS de propósito — ver a nota em PayeeContracts.

internal sealed record ImportBillRequest(
    string? DigitableLine,
    string? PixPayload,
    string SourceKind,
    DateTime ReceivedAt,
    Guid? SourceId = null,
    string? SenderAddress = null,
    string? ExternalMessageId = null);

internal sealed record ImportBillResponseContract(Guid Id, string Kind, string Rail);

internal sealed record BillOriginContract(
    string SourceKind,
    Guid? SourceId,
    string? SenderAddress,
    DateTime ReceivedAt);

internal sealed record BillContract(
    Guid Id,
    string Status,
    string Kind,
    string Rail,
    BillPartyContract? Beneficiary,
    decimal? Amount,
    DateTime? DueDate,
    string? BankCode,
    BillOriginContract Origin,
    DateTime CreatedAt);

internal sealed record BillPageContract(IReadOnlyList<BillContract> Items, string? NextCursor);

// AcknowledgeImmediateExecution: o boleto sintetico da suite ja esta VENCIDO em relogio real,
// e o ADR-017 exige o aceite de pagamento imediato para aprovar vencido (BLP.BIL35).
internal sealed record ApproveBillRequest(
    DateOnly ScheduleFor,
    string? Note,
    bool AcknowledgeRisk = false,
    bool AcknowledgeImmediateExecution = false);

internal sealed record ApproveBillResponseContract(Guid Id, string Status, DateOnly ScheduledFor);

internal sealed record BillDecisionRequest(string Reason);

internal sealed record BillDecisionResponseContract(Guid Id, string Status);

internal sealed record ValidateBillResponseContract(
    Guid Id,
    string Status,
    int BlockingFailures,
    int AttentionItems);

internal sealed record BillPartyContract(string? Name, string? TradingName, string? TaxId);

internal sealed record BillCheckContract(
    string Type,
    string Outcome,
    string Severity,
    string? ReasonCode,
    string? Evidence,
    bool IsBlockingFailure,
    DateTime EvaluatedAt);

internal sealed record BillApprovalContract(Guid DecidedBy, string Decision, DateTime DecidedAt, string? Note);

internal sealed record BillDetailContract(
    Guid Id,
    string Status,
    string Kind,
    string Rail,
    BillPartyContract? Beneficiary,
    decimal? Amount,
    decimal? OriginalAmount,
    DateTime? DueDate,
    string? BankCode,
    DateTime? MinimumScheduleDate,
    DateTime? LastConsultedAt,
    IReadOnlyList<BillCheckContract> Checks,
    BillApprovalContract? Approval,
    DateTime? ScheduledFor,
    BillOriginContract Origin,
    DateTime CreatedAt);
