namespace BillPayment.Domain.Payees;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O que o tenant espera pagar a um beneficiário. Modelada como um único VO com
/// discriminador em vez de hierarquia — mantém o mapeamento EF achatado em colunas
/// escalares, que é a lição registrada no CLAUDE.md sobre owned types aninhados.
/// </summary>
public sealed class AmountPolicy : ValueObject
{
    public const decimal MIN_TOLERANCE_PERCENT = 0m;
    public const decimal MAX_TOLERANCE_PERCENT = 100m;

    public AmountPolicyKind Kind { get; private set; } = default!;
    public Money? ExpectedAmount { get; private set; }
    public decimal? TolerancePercent { get; private set; }
    public Money? MinAmount { get; private set; }
    public Money? MaxAmount { get; private set; }

    private AmountPolicy() { }

    private AmountPolicy(AmountPolicyKind kind) => Kind = kind;

    /// <summary>Valor esperado com tolerância percentual — aluguel, mensalidade, contrato fechado.</summary>
    public static AmountPolicy Fixed(Money expectedAmount, decimal tolerancePercent)
    {
        if (expectedAmount is null)
            throw PayeeErrors.AmountPolicyAmountRequired();
        if (!expectedAmount.IsPositive)
            throw PayeeErrors.FixedAmountMustBePositive(expectedAmount.Amount);
        if (tolerancePercent < MIN_TOLERANCE_PERCENT || tolerancePercent > MAX_TOLERANCE_PERCENT)
            throw PayeeErrors.ToleranceOutOfRange(tolerancePercent);

        return new AmountPolicy(AmountPolicyKind.Fixed)
        {
            ExpectedAmount = expectedAmount,
            TolerancePercent = tolerancePercent,
        };
    }

    /// <summary>Faixa aceitável — concessionária, conta sazonal.</summary>
    public static AmountPolicy Range(Money minAmount, Money maxAmount)
    {
        if (minAmount is null || maxAmount is null)
            throw PayeeErrors.AmountPolicyAmountRequired();
        if (minAmount.IsNegative || maxAmount.IsNegative)
            throw PayeeErrors.RangeAmountCannotBeNegative();
        if (!minAmount.Currency.Equals(maxAmount.Currency))
            throw PayeeErrors.RangeCurrencyMismatch(minAmount.Currency.Name, maxAmount.Currency.Name);
        if (minAmount.Amount > maxAmount.Amount)
            throw PayeeErrors.RangeBoundsInverted(minAmount.Amount, maxAmount.Amount);

        return new AmountPolicy(AmountPolicyKind.Range)
        {
            MinAmount = minAmount,
            MaxAmount = maxAmount,
        };
    }

    /// <summary>Sem expectativa. O check de valor sai inconclusivo, nunca aprovado.</summary>
    public static AmountPolicy Unbounded() => new(AmountPolicyKind.Unbounded);

    /// <summary>
    /// Monta a política a partir dos valores crus que chegam do cadastro. Existe para que a
    /// camada de aplicação não precise escolher a factory nem compor <see cref="Money"/> —
    /// quais campos cada tipo exige é regra desta classe.
    /// </summary>
    /// <remarks>
    /// A moeda é fixa em <see cref="Currency.BRL"/>: este BC paga boleto e Pix brasileiros,
    /// e aceitar moeda pelo cadastro criaria um estado que nenhum trilho de pagamento honra.
    /// </remarks>
    public static AmountPolicy From(
        AmountPolicyKind kind,
        decimal? expectedAmount,
        decimal? tolerancePercent,
        decimal? minAmount,
        decimal? maxAmount)
    {
        if (kind is null)
            throw PayeeErrors.AmountPolicyRequired();

        if (kind == AmountPolicyKind.Unbounded)
            return Unbounded();

        if (kind == AmountPolicyKind.Fixed)
        {
            if (expectedAmount is null || tolerancePercent is null)
                throw PayeeErrors.AmountPolicyAmountRequired();

            return Fixed(new Money(expectedAmount.Value, Currency.BRL), tolerancePercent.Value);
        }

        if (minAmount is null || maxAmount is null)
            throw PayeeErrors.AmountPolicyAmountRequired();

        return Range(new Money(minAmount.Value, Currency.BRL), new Money(maxAmount.Value, Currency.BRL));
    }

    /// <summary>Um <c>true</c> aqui só significa aprovação quando <see cref="IsConclusive"/> também é verdadeiro.</summary>
    public bool IsConclusive => Kind.IsConclusive;

    public bool Matches(Money actual)
    {
        if (actual is null)
            return false;

        if (Kind == AmountPolicyKind.Unbounded)
            return true;

        if (Kind == AmountPolicyKind.Fixed)
        {
            if (!actual.Currency.Equals(ExpectedAmount!.Currency))
                return false;

            var allowed = Math.Abs(ExpectedAmount.Amount) * (TolerancePercent!.Value / 100m);
            return Math.Abs(actual.Amount - ExpectedAmount.Amount) <= allowed;
        }

        if (!actual.Currency.Equals(MinAmount!.Currency))
            return false;

        return actual.Amount >= MinAmount.Amount && actual.Amount <= MaxAmount!.Amount;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return ExpectedAmount;
        yield return TolerancePercent;
        yield return MinAmount;
        yield return MaxAmount;
    }
}
