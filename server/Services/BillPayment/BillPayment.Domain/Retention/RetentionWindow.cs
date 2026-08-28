namespace BillPayment.Domain.Retention;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Por quanto tempo o histórico de e-mails descartados é guardado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É Smart Enum e não um inteiro livre</strong> porque a janela chega pela API. Um
/// número qualquer viraria retenção arbitrária — inclusive zero, que apagaria o histórico no
/// mesmo instante em que ele nasce, e um valor enorme, que transformaria o registro em arquivo
/// permanente da caixa de entrada de alguém.
/// </para>
/// <para>
/// O <see cref="Enumeration.Id"/> <strong>é</strong> a quantidade de dias, de propósito: o valor
/// gravado no banco se lê sozinho, e a conta da purga não precisa de tabela de tradução.
/// </para>
/// </remarks>
public sealed class RetentionWindow : Enumeration
{
    public static readonly RetentionWindow SevenDays = new(7, nameof(SevenDays));
    public static readonly RetentionWindow ThirtyDays = new(30, nameof(ThirtyDays));
    public static readonly RetentionWindow NinetyDays = new(90, nameof(NinetyDays));
    public static readonly RetentionWindow OneHundredEightyDays = new(180, nameof(OneHundredEightyDays));

    /// <summary>
    /// A janela padrão de quem liga a política sem escolher — o meio da faixa.
    /// </summary>
    public static RetentionWindow Default => NinetyDays;

    /// <summary>Quantos dias a janela cobre.</summary>
    public int Days => Id;

    private RetentionWindow(int id, string name) : base(id, name) { }
}
