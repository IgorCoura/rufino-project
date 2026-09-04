namespace BillPayment.Domain.Payees;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Natureza da expectativa de valor de um beneficiário. <see cref="Unbounded"/> não prova
/// nada — o check de valor a trata como inconclusiva, nunca como aprovada.
/// </summary>
public sealed class AmountPolicyKind : Enumeration
{
    public static readonly AmountPolicyKind Fixed = new(1, nameof(Fixed), isConclusive: true);
    public static readonly AmountPolicyKind Range = new(2, nameof(Range), isConclusive: true);
    public static readonly AmountPolicyKind Unbounded = new(3, nameof(Unbounded), isConclusive: false);

    /// <summary>Se um "casou" desta política significa alguma coisa para o check de valor.</summary>
    public bool IsConclusive { get; }

    private AmountPolicyKind(int id, string name, bool isConclusive) : base(id, name)
    {
        IsConclusive = isConclusive;
    }
}
