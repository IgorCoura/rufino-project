namespace TenantManagement.IntegrationTests.Mothers;

using TenantManagement.IntegrationTests.Contracts;

internal static class TenantRequestMother
{
    public const string CompanyName = "RUFINO EMPREITEIRA LTDA";
    public const string IndividualName = "IGOR DE BRITO COURA";
    public const string OwnerEmail = "titular@rufino.com.br";

    /// <summary>Dígitos verificadores conferidos à mão.</summary>
    public const string Cnpj = "11222333000181";
    public const string OtherCnpj = "11444777000161";
    public const string Cpf = "52998224725";
    public const string OtherCpf = "11144477735";

    public static AddressRequest Address(
        string zipCode = "01310-100",
        string city = "Sao Paulo",
        string state = "SP")
        => new(zipCode, "Avenida Paulista", "1000", "Conj. 51", "Bela Vista", city, state, null);

    public static RegisterTenantRequest Company(
        string? legalName = null,
        string? taxId = null,
        string? ownerEmail = null,
        IReadOnlyList<string>? products = null,
        Guid? id = null,
        AddressRequest? address = null,
        string? tradeName = null)
        => new(
            "Company",
            legalName ?? CompanyName,
            tradeName ?? "RUFINO",
            taxId ?? Cnpj,
            "contato@rufino.com.br",
            "11987654321",
            address ?? Address(),
            ownerEmail ?? OwnerEmail,
            products,
            id);

    public static RegisterTenantRequest Individual(
        string? legalName = null,
        string? taxId = null,
        string? ownerEmail = null,
        IReadOnlyList<string>? products = null)
        => new(
            "Individual",
            legalName ?? IndividualName,
            null,
            taxId ?? Cpf,
            "igor@rufino.com.br",
            "11987654321",
            Address(),
            ownerEmail ?? "igor@rufino.com.br",
            products,
            null);
}
