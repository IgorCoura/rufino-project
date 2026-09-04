namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;

/// <summary>
/// O papel de alguém DENTRO do tenant. Permissão fina não mora aqui — mora no provedor de
/// identidade, que é quem os endpoints consultam. Aqui só existe a distinção que o cadastro
/// precisa manter: quem responde pela conta e quem apenas participa dela.
/// </summary>
public sealed class MembershipRole : Enumeration
{
    public static readonly MembershipRole Owner = new(1, nameof(Owner));
    public static readonly MembershipRole Member = new(2, nameof(Member));

    private MembershipRole(int id, string name) : base(id, name) { }
}
