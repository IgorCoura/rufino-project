namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// As condições sob as quais uma aprovação é aceita.
/// </summary>
/// <remarks>
/// <para>
/// Entra como parâmetro de <c>Bill.Approve</c> em vez de morar no agregado porque não é
/// propriedade do boleto: a mesma conta pode ser aprovável por uma pessoa e não por outra, e o
/// prazo de validade do retrato é decisão de operação, não do documento.
/// </para>
/// <para>
/// <strong>A alçada é por aprovador — mas hoje só existe teto único.</strong> Amarrar limite a
/// pessoa exige a identidade vinda do provedor de identidade, que entra na fase 6 junto com o
/// Keycloak. Até lá o handler resolve um teto de configuração e o passa aqui; o formato do VO
/// já é o final, então o dia da amarração muda de onde o valor vem, não o domínio.
/// </para>
/// </remarks>
public sealed class ApprovalPolicy : ValueObject
{
    /// <summary>Prazo de validade do retrato da consulta, em horas. Valor do doc 03.</summary>
    public const int DEFAULT_MAX_SNAPSHOT_AGE_HOURS = 12;

    public TimeSpan MaxSnapshotAge { get; private set; }

    /// <summary>Teto do aprovador. Nulo significa sem teto — não significa zero.</summary>
    public Money? Limit { get; private set; }

    private ApprovalPolicy() { }

    public static ApprovalPolicy Of(TimeSpan maxSnapshotAge, Money? limit = null)
        => maxSnapshotAge <= TimeSpan.Zero
            ? throw BillErrors.ApprovalPolicyWithoutSnapshotWindow()
            : new ApprovalPolicy { MaxSnapshotAge = maxSnapshotAge, Limit = limit };

    public static ApprovalPolicy Default(Money? limit = null)
        => Of(TimeSpan.FromHours(DEFAULT_MAX_SNAPSHOT_AGE_HOURS), limit);

    public bool Allows(Money amount)
        => Limit is null || amount is null || amount.Amount <= Limit.Amount;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxSnapshotAge;
        yield return Limit;
    }
}
