namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;

/// <summary>
/// Natureza do tenant. É o único lugar do BC onde pessoa física e jurídica se diferenciam:
/// o tipo de documento que o cadastro exige e o direito a nome fantasia. Nenhuma regra de
/// produto deve voltar a ler isto para decidir comportamento.
/// </summary>
public sealed class TenantKind : Enumeration
{
    public static readonly TenantKind Individual = new(1, nameof(Individual), TaxIdKind.CPF, allowsTradeName: false);
    public static readonly TenantKind Company = new(2, nameof(Company), TaxIdKind.CNPJ, allowsTradeName: true);

    /// <summary>Tipo de documento que o cadastro principal precisa ter.</summary>
    public TaxIdKind ExpectedPrimaryTaxIdKind { get; }

    /// <summary>Nome fantasia é de pessoa jurídica; pessoa física tem nome e mais nada.</summary>
    public bool AllowsTradeName { get; }

    private TenantKind(int id, string name, TaxIdKind expectedPrimaryTaxIdKind, bool allowsTradeName)
        : base(id, name)
    {
        ExpectedPrimaryTaxIdKind = expectedPrimaryTaxIdKind;
        AllowsTradeName = allowsTradeName;
    }
}
