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
    DateTime UpdatedAt,

    /// <summary>
    /// O status <strong>como o provedor o escreveu</strong>. O catálogo dele é maior que o
    /// nosso, e sem isto a tela dizia "aceito pelo provedor" para uma ordem parada esperando
    /// alguém liberar a autorização de ação crítica.
    /// </summary>
    string? ProviderRawStatus = null,

    /// <summary>
    /// <c>false</c> = travado esperando a autorização de ação crítica do provedor (o código no
    /// celular). Nulo = o provedor não se pronunciou.
    /// </summary>
    bool? ProviderAuthorized = null);

public sealed record PaymentOrderPage(IReadOnlyList<PaymentOrderDto> Items, string? NextCursor);
