namespace BillPayment.UnitTests.PayerProfiles;

using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

public class PayerKindTests
{
    // Pessoa física espera CPF; pessoa jurídica espera CNPJ.
    [Fact]
    public void ExpectedPrimaryTaxIdKind_ShouldMatchThePayerNature()
    {
        Assert.Same(TaxIdKind.CPF, PayerKind.Individual.ExpectedPrimaryTaxIdKind);
        Assert.Same(TaxIdKind.CNPJ, PayerKind.Company.ExpectedPrimaryTaxIdKind);
    }

    // Só pessoa jurídica tem raiz de CNPJ — filial de pessoa física não existe.
    [Fact]
    public void SupportsCnpjRootMatching_ShouldBeTrueOnlyForCompany()
    {
        Assert.False(PayerKind.Individual.SupportsCnpjRootMatching);
        Assert.True(PayerKind.Company.SupportsCnpjRootMatching);
    }

    // O catálogo tem exatamente duas naturezas de pagador.
    [Fact]
    public void GetAll_ShouldReturnTwoKinds()
    {
        Assert.Equal(2, Enumeration.GetAll<PayerKind>().Count());
    }
}
