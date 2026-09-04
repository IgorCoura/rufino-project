namespace BillPayment.UnitTests.Lookups;

using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

public class LookupPartyTests
{
    private const string VALID_CNPJ = "11222333000181";
    private const string LEGAL_NAME = "SABESP SANEAMENTO BASICO DO ESTADO DE SP S.A.";
    private const string TRADING_NAME = "SABESP";

    // O caso completo: razão social, nome fantasia e documento, como a cobrança bancária promete devolver.
    [Fact]
    public void From_WithNameTradingNameAndTaxId_ShouldKeepAllThree()
    {
        var party = LookupParty.From(LEGAL_NAME, TRADING_NAME, VALID_CNPJ);

        Assert.Equal(LEGAL_NAME, party.Name);
        Assert.Equal(TRADING_NAME, party.TradingName);
        Assert.Equal(VALID_CNPJ, party.TaxId!.Value);
        Assert.True(party.HasTaxId);
    }

    // O caso medido da arrecadação: só nome comercial volta, sem documento — e isso é uma parte
    // válida, porque o check degrada para cotejo por nome em vez de ficar sem consulta.
    [Fact]
    public void From_WithOnlyTradingName_ShouldStillBuildTheParty()
    {
        var party = LookupParty.From(name: null, tradingName: TRADING_NAME, taxId: null);

        Assert.Null(party.Name);
        Assert.Equal(TRADING_NAME, party.TradingName);
        Assert.False(party.HasTaxId);
        Assert.Equal(TRADING_NAME, party.DisplayName);
    }

    // Documento que não passa no dígito verificador vira ausência, não exceção: um CNPJ
    // corrompido na resposta do provedor não pode derrubar a consulta inteira.
    [Theory]
    [InlineData("11222333000180")]
    [InlineData("00000000000000")]
    [InlineData("nao-e-documento")]
    [InlineData("")]
    public void From_WithUnreadableTaxId_ShouldDegradeToNameOnly(string taxId)
    {
        var party = LookupParty.From(LEGAL_NAME, TRADING_NAME, taxId);

        Assert.False(party.HasTaxId);
        Assert.Equal(LEGAL_NAME, party.Name);
    }

    // Sem nome, sem nome fantasia e sem documento não há beneficiário nenhum — BLP.LKP01.
    [Fact]
    public void Of_WithoutAnyIdentifier_ShouldThrow_BLP_LKP01()
    {
        var ex = Assert.Throws<DomainException>(() => LookupParty.Of(name: null, tradingName: "  ", taxId: null));

        Assert.Equal("BLP.LKP01", ex.Id);
    }

    // A razão social vem antes do nome fantasia na exibição — é ela que o cadastro do Payee guarda.
    [Fact]
    public void DisplayName_WithBothNames_ShouldPreferTheLegalName()
    {
        var party = LookupParty.From(LEGAL_NAME, TRADING_NAME, null);

        Assert.Equal(LEGAL_NAME, party.DisplayName);
    }

    // Espaços em volta são aparados; o resto do texto fica intacto, porque o cotejo com o
    // cadastro é de Payee.MatchesName e uma segunda normalização aqui divergiria dele.
    [Fact]
    public void Of_WithPaddedName_ShouldTrimWithoutOtherChanges()
    {
        var party = LookupParty.Of("   Padaria São José Ltda.   ");

        Assert.Equal("Padaria São José Ltda.", party.Name);
    }

    // Nome absurdamente longo é truncado em vez de derrubar a consulta — o campo é evidência,
    // não identidade, e o documento é que decide o check quando existe.
    [Fact]
    public void Of_WithOverlongName_ShouldTruncateToTheMaxLength()
    {
        var party = LookupParty.Of(new string('A', LookupParty.NAME_MAX_LENGTH + 50));

        Assert.Equal(LookupParty.NAME_MAX_LENGTH, party.Name!.Length);
    }

    // Igualdade por valor: mesmo conteúdo, mesma parte.
    [Fact]
    public void Equals_WithSameContent_ShouldBeTrue()
    {
        var a = LookupParty.From(LEGAL_NAME, TRADING_NAME, VALID_CNPJ);
        var b = LookupParty.Of(LEGAL_NAME, TRADING_NAME, TaxId.Parse(VALID_CNPJ));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
