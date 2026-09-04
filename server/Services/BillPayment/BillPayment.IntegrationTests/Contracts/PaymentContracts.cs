namespace BillPayment.IntegrationTests.Contracts;

// DTOs duplicados de propósito, como os demais: o teste afirma o CONTRATO da API, e reusar o
// DTO da Application deixaria uma mudança de contrato passar sem quebrar teste nenhum.

internal sealed record PaymentOrderContract(
    Guid Id,
    Guid BillId,
    string Rail,
    string Status,
    string Hold,
    DateOnly RequestedScheduleDate,
    DateOnly? EffectiveScheduleDate,
    decimal? Amount,
    decimal? Fee,
    DateOnly? PaidAt,
    IReadOnlyList<string> FailReasons,
    string? LastError,
    int SubmissionAttempts,
    bool RequiresConfirmation,
    Guid? ConfirmedBy,
    bool HasReceipt);

internal sealed record PaymentOrderPageContract(
    IReadOnlyList<PaymentOrderContract> Items,
    string? NextCursor);
