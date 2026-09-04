namespace BillPayment.Application.Queries.PaymentOrders;

/// <summary>
/// A ordem de pagamento como a tela a vê. Sem instrumento, sem URL de provedor — o que
/// identifica o compromisso é o boleto, e o comprovante sai por endpoint próprio.
/// </summary>
public sealed record PaymentOrderDto(
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
    IReadOnlyCollection<string> FailReasons,
    string? LastError,
    int SubmissionAttempts,
    bool RequiresConfirmation,
    Guid? ConfirmedBy,
    bool HasReceipt,
    DateTimeOffset? LastProviderSyncAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PaymentOrderPage(IReadOnlyList<PaymentOrderDto> Items, string? NextCursor);
