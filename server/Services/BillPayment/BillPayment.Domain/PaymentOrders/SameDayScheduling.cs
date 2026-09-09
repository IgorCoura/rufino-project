namespace BillPayment.Domain.PaymentOrders;

/// <summary>
/// Hoje ainda serve como data de pagamento, neste instante? — e, quando não serve, por quê.
/// </summary>
/// <remarks>
/// <para>
/// Existe porque "pagar hoje" é a ÚNICA data que depende da hora (ADR-021). A fila só fala com o
/// provedor dentro da janela; fora dela a próxima submissão possível é na abertura seguinte, e
/// aí a data "hoje" já é passado. Bloquear na hora de escolher é dizer isso de frente, em vez
/// de aceitar a escolha e deslizar um dia sem avisar.
/// </para>
/// <para>
/// O motivo é um <strong>código</strong>, não uma frase: o mesmo veredito alimenta a recusa do
/// agregado (<c>BLP.BIL36</c>, que formata a mensagem) e a lista de opções da tela (que traduz
/// no idioma dela). Texto pronto no domínio amarraria as duas.
/// </para>
/// </remarks>
public sealed record SameDayScheduling(bool Allowed, string? ReasonCode)
{
    /// <summary>Fora da janela de submissão: a fila só submeteria amanhã.</summary>
    public const string OUTSIDE_WINDOW = "outside_window";

    /// <summary>Hoje não é dia útil: o provedor não liquida hoje de qualquer forma.</summary>
    public const string NOT_A_WORKING_DAY = "not_a_working_day";

    /// <summary>Hoje é anterior à primeira data que o provedor aceita para este boleto.</summary>
    public const string PROVIDER_MINIMUM = "provider_minimum";

    public static SameDayScheduling Allow() => new(true, null);

    public static SameDayScheduling Deny(string reasonCode) => new(false, reasonCode);
}
