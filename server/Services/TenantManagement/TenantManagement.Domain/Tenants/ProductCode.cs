namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;

/// <summary>
/// Os produtos da plataforma que um tenant pode habilitar. O cadastro sabe QUE o produto
/// está habilitado; o que cada produto faz com isso é assunto do produto.
/// </summary>
public sealed class ProductCode : Enumeration
{
    public static readonly ProductCode PeopleManagement = new(1, nameof(PeopleManagement));
    public static readonly ProductCode BillPayment = new(2, nameof(BillPayment));

    private ProductCode(int id, string name) : base(id, name) { }
}
