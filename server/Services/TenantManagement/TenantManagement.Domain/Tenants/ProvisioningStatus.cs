namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;

/// <summary>
/// Estado da concessão de acesso no provedor de identidade. Existe porque o provedor não
/// participa da transação do banco: o cadastro commita, a concessão pode falhar, e sem este
/// campo o tenant ficaria cadastrado e inacessível sem ninguém saber.
/// </summary>
public sealed class ProvisioningStatus : Enumeration
{
    public static readonly ProvisioningStatus Pending = new(1, nameof(Pending));
    public static readonly ProvisioningStatus Done = new(2, nameof(Done));
    public static readonly ProvisioningStatus Failed = new(3, nameof(Failed));

    private ProvisioningStatus(int id, string name) : base(id, name) { }
}
