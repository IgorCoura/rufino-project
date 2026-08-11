namespace BillPayment.Domain.PayerProfiles;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Natureza do tenant. A diferença entre PF e PJ mora aqui, no tipo de subconta do
/// provedor e na alçada — em nenhum outro lugar do domínio.
/// </summary>
public sealed class PayerKind : Enumeration
{
    public static readonly PayerKind Individual = new(1, nameof(Individual), TaxIdKind.CPF, supportsCnpjRootMatching: false);
    public static readonly PayerKind Company = new(2, nameof(Company), TaxIdKind.CNPJ, supportsCnpjRootMatching: true);

    /// <summary>Tipo de documento que o cadastro principal precisa ter.</summary>
    public TaxIdKind ExpectedPrimaryTaxIdKind { get; }

    /// <summary>Só PJ tem raiz de CNPJ; filial de PF não existe.</summary>
    public bool SupportsCnpjRootMatching { get; }

    private PayerKind(int id, string name, TaxIdKind expectedPrimaryTaxIdKind, bool supportsCnpjRootMatching)
        : base(id, name)
    {
        ExpectedPrimaryTaxIdKind = expectedPrimaryTaxIdKind;
        SupportsCnpjRootMatching = supportsCnpjRootMatching;
    }
}
