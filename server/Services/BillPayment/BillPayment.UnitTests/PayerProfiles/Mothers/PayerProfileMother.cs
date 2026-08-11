namespace BillPayment.UnitTests.PayerProfiles.Mothers;

using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SharedKernel;

internal static class PayerProfileMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));

    public const string DefaultLegalName = "RUFINO EMPREITEIRA LTDA";

    /// <summary>Matriz — raiz 11222333, filial 0001. Dígitos verificadores conferidos à mão.</summary>
    public const string HeadquartersCnpj = "11222333000181";

    /// <summary>Filial da mesma raiz 11222333 — usada para exercitar o casamento por raiz.</summary>
    public const string BranchCnpj = "11222333000262";

    /// <summary>Raiz diferente (11444777) — não pode casar por raiz com a matriz acima.</summary>
    public const string ForeignCnpj = "11444777000161";

    public const string DefaultCpf = "52998224725";

    public static TaxId Cnpj(string value = HeadquartersCnpj) => new(value, TaxIdKind.CNPJ);

    public static TaxId Cpf(string value = DefaultCpf) => new(value, TaxIdKind.CPF);

    /// <summary>Caminho feliz: omitir um parâmetro aplica o default do cenário (PJ).</summary>
    public static PayerProfile Register(
        PayerKind? kind = null,
        string? legalName = null,
        TaxId? primaryTaxId = null,
        DateTime? occurredAt = null,
        TenantId? tenantId = null)
        => RegisterVerbatim(
            kind ?? PayerKind.Company,
            legalName ?? DefaultLegalName,
            primaryTaxId ?? Cnpj(),
            occurredAt,
            tenantId);

    /// <summary>
    /// Repassa os argumentos sem coalescer — único caminho capaz de exercitar as
    /// invariantes que rejeitam nulos.
    /// </summary>
    public static PayerProfile RegisterVerbatim(
        PayerKind kind,
        string legalName,
        TaxId primaryTaxId,
        DateTime? occurredAt = null,
        TenantId? tenantId = null)
        => PayerProfile.Register(
            tenantId ?? DefaultTenant,
            kind,
            legalName,
            primaryTaxId,
            occurredAt ?? DefaultOccurredAt);

    public static PayerProfile Individual()
        => Register(PayerKind.Individual, "IGOR DE BRITO COURA", Cpf());

    public static PayerProfile CompanyWithRootMatching()
    {
        var profile = Register();
        profile.EnableCnpjRootMatching(DefaultOccurredAt);
        return profile;
    }
}
