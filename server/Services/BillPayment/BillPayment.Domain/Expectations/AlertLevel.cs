namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.SeedWork;

/// <summary>O escalonamento de um ciclo não cumprido.</summary>
/// <remarks>
/// <strong>Um alerta por nível por ciclo, nunca repetido.</strong> Repetir o mesmo nível é o
/// caminho mais curto para o usuário aprender a ignorar o alerta — e alerta ignorado é pior que
/// alerta nenhum, porque dá a impressão de que alguém está olhando.
/// </remarks>
public sealed class AlertLevel : Enumeration
{
    public static readonly AlertLevel HeadsUp = new(1, nameof(HeadsUp), daysBeforeDue: null);
    public static readonly AlertLevel Warning = new(2, nameof(Warning), daysBeforeDue: 3);
    public static readonly AlertLevel Urgent = new(3, nameof(Urgent), daysBeforeDue: 0);
    public static readonly AlertLevel Overdue = new(4, nameof(Overdue), daysBeforeDue: null);

    /// <summary>
    /// Dias antes do vencimento em que o nível dispara. Nulo no <see cref="HeadsUp"/>, cuja data
    /// é <strong>aprendida</strong> por expectativa — conta que chega em cima da hora e conta que
    /// chega com folga não podem avisar no mesmo dia —, e no <see cref="Overdue"/>, que é
    /// qualquer dia depois do vencimento.
    /// </summary>
    public int? DaysBeforeDue { get; }

    private AlertLevel(int id, string name, int? daysBeforeDue) : base(id, name)
        => DaysBeforeDue = daysBeforeDue;

    /// <summary>
    /// O nível devido em <paramref name="today"/>, ou <c>null</c> quando ainda não é hora.
    /// </summary>
    /// <remarks>
    /// Devolve sempre o nível <em>mais alto</em> alcançado: um job que ficou dois dias parado
    /// deve mandar o alerta que vale hoje, não a fila dos que ficaram para trás.
    /// </remarks>
    public static AlertLevel? DueOn(DateOnly today, DateOnly alertAt, DateOnly expectedDueDate)
    {
        if (today > expectedDueDate)
            return Overdue;
        if (today == expectedDueDate)
            return Urgent;
        if (today >= expectedDueDate.AddDays(-Warning.DaysBeforeDue!.Value))
            return Warning;

        return today >= alertAt ? HeadsUp : null;
    }
}
