namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.SeedWork;

/// <summary>Com que frequência a conta é esperada.</summary>
/// <remarks>
/// O intervalo em meses é o que o domínio usa para abrir o ciclo seguinte e para conferir o teto
/// de antecedência do alerta — avisar com mais antecedência que o próprio intervalo faria o aviso
/// de um ciclo nascer antes de o anterior fechar.
/// </remarks>
public sealed class Recurrence : Enumeration
{
    /// <summary>
    /// Tolerância sobre o intervalo nominal ao deduzir a recorrência do histórico.
    /// </summary>
    /// <remarks>
    /// Oito dias: mês tem 28 a 31 dias e vencimento é empurrado para dia útil, então exigir
    /// exatidão descartaria conta perfeitamente regular. Frouxo demais faria compra avulsa virar
    /// expectativa e gerar alerta que ninguém pediu.
    /// </remarks>
    private const int TOLERANCE_DAYS = 8;

    public static readonly Recurrence Monthly = new(1, nameof(Monthly), intervalMonths: 1);
    public static readonly Recurrence Bimonthly = new(2, nameof(Bimonthly), intervalMonths: 2);
    public static readonly Recurrence Quarterly = new(3, nameof(Quarterly), intervalMonths: 3);
    public static readonly Recurrence Annual = new(4, nameof(Annual), intervalMonths: 12);

    public int IntervalMonths { get; }

    /// <summary>Dias do intervalo, para o teto de antecedência do alerta.</summary>
    public int IntervalDays => IntervalMonths * 30;

    private Recurrence(int id, string name, int intervalMonths) : base(id, name)
        => IntervalMonths = intervalMonths;

    /// <summary>
    /// A recorrência cujo intervalo mais se aproxima do espaçamento observado, ou <c>null</c>
    /// quando nenhuma fica dentro da tolerância — e nesse caso não há expectativa a aprender.
    /// </summary>
    public static Recurrence? ClosestTo(int intervalDays)
    {
        Recurrence? best = null;
        var smallestGap = int.MaxValue;

        foreach (var candidate in GetAll<Recurrence>())
        {
            var gap = Math.Abs(intervalDays - candidate.IntervalDays);
            if (gap > TOLERANCE_DAYS || gap >= smallestGap)
                continue;

            smallestGap = gap;
            best = candidate;
        }

        return best;
    }
}
