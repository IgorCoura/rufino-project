namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.SeedWork;

/// <summary>De onde a expectativa veio.</summary>
public sealed class ExpectationOrigin : Enumeration
{
    /// <summary>Deduzida do histórico de boletos do próprio tenant.</summary>
    public static readonly ExpectationOrigin Learned = new(1, nameof(Learned));

    /// <summary>Cadastrada por uma pessoa — cobre o que o histórico não alcança.</summary>
    public static readonly ExpectationOrigin Manual = new(2, nameof(Manual));

    private ExpectationOrigin(int id, string name) : base(id, name) { }
}
