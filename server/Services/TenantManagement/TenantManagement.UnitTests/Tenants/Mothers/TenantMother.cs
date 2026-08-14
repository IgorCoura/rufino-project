namespace TenantManagement.UnitTests.Tenants.Mothers;

using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;

internal static class TenantMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultId = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));

    public const string DefaultCompanyName = "RUFINO EMPREITEIRA LTDA";
    public const string DefaultTradeName = "RUFINO";
    public const string DefaultIndividualName = "IGOR DE BRITO COURA";
    public const string DefaultOwnerEmail = "titular@rufino.com.br";

    /// <summary>Dígitos verificadores conferidos à mão — raiz 11222333, filial 0001.</summary>
    public const string DefaultCnpj = "11222333000181";
    public const string DefaultCpf = "52998224725";

    public static TaxId Cnpj(string value = DefaultCnpj) => new(value, TaxIdKind.CNPJ);

    public static TaxId Cpf(string value = DefaultCpf) => new(value, TaxIdKind.CPF);

    public static ContactInfo Contact(string email = "contato@rufino.com.br", string? phone = "11987654321")
        => ContactInfo.Create(email, phone);

    public static Address Address(
        string zipCode = "01310100",
        string street = "Avenida Paulista",
        string number = "1000",
        string? complement = "Conjunto 51",
        string neighborhood = "Bela Vista",
        string city = "Sao Paulo",
        string state = "SP",
        string? country = null)
        => Domain.SharedKernel.Address.Create(zipCode, street, number, complement, neighborhood, city, state, country);

    /// <summary>Caminho feliz: omitir um parâmetro aplica o default do cenário (PJ ativa).</summary>
    public static Tenant Register(
        TenantKind? kind = null,
        string? legalName = null,
        string? tradeName = null,
        TaxId? primaryTaxId = null,
        ContactInfo? contact = null,
        Address? address = null,
        string? ownerEmail = null,
        DateTime? occurredAt = null,
        TenantId? id = null)
        => RegisterVerbatim(
            kind ?? TenantKind.Company,
            legalName ?? DefaultCompanyName,
            tradeName,
            primaryTaxId ?? Cnpj(),
            contact ?? Contact(),
            address ?? Address(),
            ownerEmail ?? DefaultOwnerEmail,
            occurredAt,
            id);

    /// <summary>
    /// Repassa os argumentos sem coalescer — único caminho capaz de exercitar as
    /// invariantes que rejeitam nulos.
    /// </summary>
    public static Tenant RegisterVerbatim(
        TenantKind kind,
        string legalName,
        string? tradeName,
        TaxId primaryTaxId,
        ContactInfo contact,
        Address address,
        string ownerEmail,
        DateTime? occurredAt = null,
        TenantId? id = null)
        => Tenant.Register(
            id ?? DefaultId,
            kind,
            legalName,
            tradeName,
            primaryTaxId,
            contact,
            address,
            ownerEmail,
            occurredAt ?? DefaultOccurredAt);

    public static Tenant Individual(string? ownerEmail = null)
        => Register(TenantKind.Individual, DefaultIndividualName, null, Cpf(), ownerEmail: ownerEmail);

    /// <summary>PJ com o cadastro já provisionado e sem eventos pendentes — ponto de partida limpo.</summary>
    public static Tenant Provisioned(DateTime? occurredAt = null)
    {
        var tenant = Register(occurredAt: occurredAt);
        tenant.ConfirmAccessProvisioned(DefaultOwnerEmail, UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000aa")), occurredAt ?? DefaultOccurredAt);
        tenant.ClearDomainEvents();
        return tenant;
    }
}
