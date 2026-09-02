namespace BillPayment.Domain.PaymentOrders;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// O retrato de uma ordem como o provedor a enxerga, já traduzido para o vocabulário do Domain.
/// É o veículo entre o adapter e o agregado — nunca é persistido.
/// </summary>
/// <remarks>
/// <para>
/// <c>RawStatus</c> viaja junto do <see cref="Status"/> mapeado porque o catálogo do provedor é
/// maior que o nosso (a análise de risco dele, por exemplo, cai em <c>Pending</c>) e a evidência
/// precisa do nome original para diagnóstico — o mapeamento mora no adapter, a prova fica aqui.
/// </para>
/// <para>
/// <strong><c>ReceiptUrl</c> é credencial ao portador</strong>, como toda URL de documento deste
/// BC: trafega em memória para o passo que baixa o comprovante e <strong>não é persistida nem
/// logada</strong> — o que fica guardado é o arquivo no balde, sob a chave do tenant.
/// </para>
/// </remarks>
public sealed record ProviderPaymentSnapshot(
    string ProviderOrderId,
    PaymentOrderStatus Status,
    string RawStatus,
    DateOnly? EffectiveScheduleDate,
    DateOnly? PaidAt,
    Money? Fee,
    IReadOnlyCollection<string> FailReasons,
    string? ReceiptUrl);
