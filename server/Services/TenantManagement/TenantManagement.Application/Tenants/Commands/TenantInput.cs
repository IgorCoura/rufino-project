namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;

/// <summary>
/// Endereço como ele chega pela borda — texto puro. Vira <see cref="Address"/> no handler,
/// que é onde as invariantes do VO reprovam o que não presta.
/// </summary>
public sealed record AddressInput(
    string ZipCode,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    string? Country)
{
    public Address ToAddress()
        => Address.Create(ZipCode, Street, Number, Complement, Neighborhood, City, State, Country);
}

/// <summary>
/// Traduz os textos que chegam pela borda nos Smart Enums do domínio. Existe para que um
/// valor desconhecido vire erro de domínio — e portanto 400 com código — em vez de a
/// <c>InvalidOperationException</c> do <c>FromDisplayName</c> subir como 500.
/// </summary>
internal static class TenantInput
{
    public static TenantKind ParseKind(string? value)
        => Enumeration.TryFromDisplayName<TenantKind>(value ?? string.Empty)
           ?? throw TenantErrors.UnknownKind(value ?? string.Empty);

    public static ProductCode ParseProduct(string? value)
        => Enumeration.TryFromDisplayName<ProductCode>(value ?? string.Empty)
           ?? throw TenantErrors.UnknownProduct(value ?? string.Empty);

    public static MembershipRole ParseRole(string? value)
        => Enumeration.TryFromDisplayName<MembershipRole>(value ?? string.Empty)
           ?? throw TenantErrors.UnknownMembershipRole(value ?? string.Empty);
}
