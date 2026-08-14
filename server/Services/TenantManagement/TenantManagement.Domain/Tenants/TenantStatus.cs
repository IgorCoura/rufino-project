namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;

public sealed class TenantStatus : Enumeration
{
    public static readonly TenantStatus Active = new(1, nameof(Active));

    /// <summary>Cadastro preservado, acesso cortado. Suspender não apaga nem libera o documento.</summary>
    public static readonly TenantStatus Suspended = new(2, nameof(Suspended));

    private TenantStatus(int id, string name) : base(id, name) { }
}
