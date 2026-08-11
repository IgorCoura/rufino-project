namespace BillPayment.UnitTests.Payees.Mothers;

using BillPayment.Domain.Payees;
using BillPayment.Domain.SharedKernel;

internal static class PayeeMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));

    public const string DefaultLegalName = "SECONCI SAO PAULO";
    public const string DefaultCnpj = "11222333000181";

    public static Money Brl(decimal amount) => new(amount, Currency.BRL);

    public static TaxId Cnpj(string value = DefaultCnpj) => new(value, TaxIdKind.CNPJ);

    /// <summary>Caminho feliz: omitir um parâmetro aplica o default do cenário.</summary>
    public static Payee Register(
        string? legalName = null,
        TaxId? taxId = null,
        AmountPolicy? amountPolicy = null,
        DateTime? occurredAt = null,
        TenantId? tenantId = null)
        => RegisterVerbatim(
            legalName ?? DefaultLegalName,
            taxId ?? Cnpj(),
            amountPolicy ?? AmountPolicy.Unbounded(),
            occurredAt,
            tenantId);

    /// <summary>
    /// Repassa os argumentos sem coalescer — único caminho capaz de exercitar as
    /// invariantes que rejeitam nulos. <see cref="Register"/> substituiria pelo default.
    /// </summary>
    public static Payee RegisterVerbatim(
        string legalName,
        TaxId taxId,
        AmountPolicy amountPolicy,
        DateTime? occurredAt = null,
        TenantId? tenantId = null)
        => Payee.Register(
            tenantId ?? DefaultTenant,
            legalName,
            taxId,
            amountPolicy,
            occurredAt ?? DefaultOccurredAt);

    public static Payee WithFixedAmount(decimal expected, decimal tolerancePercent)
        => Register(amountPolicy: AmountPolicy.Fixed(Brl(expected), tolerancePercent));

    public static Payee WithRange(decimal min, decimal max)
        => Register(amountPolicy: AmountPolicy.Range(Brl(min), Brl(max)));

    public static Payee Inactive()
    {
        var payee = Register();
        payee.Deactivate(DefaultOccurredAt);
        return payee;
    }
}
