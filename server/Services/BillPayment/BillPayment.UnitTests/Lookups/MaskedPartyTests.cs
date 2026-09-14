namespace BillPayment.UnitTests.Lookups;

using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

public class MaskedPartyTests
{
    private const string CPF = "52998224725";
    private const string CNPJ = "11222333000181";

    // A pontuação sai e o que não é dígito vira máscara, venha como '*', 'X' ou outro caractere.
    [Theory]
    [InlineData("***.982.247-**", "***982247**")]
    [InlineData("529.***.***-25", "529******25")]
    [InlineData("XXX.982.247-XX", "***982247**")]
    [InlineData("11.222.***/0001-**", "11222***0001**")]
    public void Of_WithMaskedDocument_ShouldNormalizeToDigitsAndMask(string raw, string expected)
    {
        var payer = MaskedParty.Of("Fulano", raw);

        Assert.Equal(expected, payer.MaskedTaxId);
    }

    // Máscara sem nenhum dígito visível não contradiz nada; guardá-la só criaria a ilusão de
    // que o pagador foi conferido — BLP.LKP06.
    [Theory]
    [InlineData("***.***.***-**")]
    [InlineData("**************")]
    public void Of_WithFullyMaskedDocument_ShouldThrow_BLP_LKP06(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => MaskedParty.Of("Fulano", raw));

        Assert.Equal("BLP.LKP06", ex.Id);
    }

    // Dígitos visíveis que batem com o documento do tenant não contradizem — e também não
    // confirmam nada, que é o ponto do ADR-004.
    [Fact]
    public void IsCompatibleWith_WhenVisibleDigitsMatch_ShouldReturnTrue()
    {
        var payer = MaskedParty.Of("Fulano", "***.982.247-**");

        Assert.True(payer.IsCompatibleWith(TaxId.Parse(CPF)));
    }

    // Um dígito visível que difere é contradição comprovada — é o único caso que bloqueia.
    [Fact]
    public void IsCompatibleWith_WhenAVisibleDigitDiffers_ShouldReturnFalse()
    {
        var payer = MaskedParty.Of("Fulano", "***.982.999-**");

        Assert.False(payer.IsCompatibleWith(TaxId.Parse(CPF)));
    }

    // Comprimento diferente é ausência de conclusão, não contradição: não há garantia de que o
    // provedor preserve o comprimento, e travar pagamento legítimo por formato de máscara é
    // pior do que deixar o check inconclusivo.
    [Fact]
    public void IsCompatibleWith_WhenMaskLengthDiffersFromTheDocument_ShouldReturnTrue()
    {
        var payer = MaskedParty.Of("Fulano", "***.982.247-**");

        Assert.True(payer.IsCompatibleWith(TaxId.Parse(CNPJ)));
    }

    // Sem documento mascarado não há o que comparar.
    [Fact]
    public void IsCompatibleWith_WithoutAMaskedDocument_ShouldReturnTrue()
    {
        var payer = MaskedParty.Of("Fulano", maskedTaxId: null);

        Assert.True(payer.IsCompatibleWith(TaxId.Parse(CPF)));
        Assert.Equal(0, payer.VisibleDigitCount);
    }

    // Basta ser compatível com UM dos documentos do tenant — o principal ou os adicionais.
    [Fact]
    public void IsCompatibleWithAny_WhenOneOfTheTenantDocumentsMatches_ShouldReturnTrue()
    {
        var payer = MaskedParty.Of("Fulano", "***.982.247-**");

        Assert.True(payer.IsCompatibleWithAny([TaxId.Parse(CNPJ), TaxId.Parse(CPF)]));
    }

    // Nenhum documento do tenant sobrevive à comparação: contradição.
    [Fact]
    public void IsCompatibleWithAny_WhenEveryComparableDocumentDiffers_ShouldReturnFalse()
    {
        var payer = MaskedParty.Of("Fulano", "***.111.222-**");

        Assert.False(payer.IsCompatibleWithAny([TaxId.Parse(CPF)]));
    }

    // Tenant sem documento cadastrado não tem contra o que contradizer.
    [Fact]
    public void IsCompatibleWithAny_WithNoTenantDocuments_ShouldReturnTrue()
    {
        var payer = MaskedParty.Of("Fulano", "***.982.247-**");

        Assert.True(payer.IsCompatibleWithAny([]));
    }

    // Documento inteiro e com DV válido sai com a pontuação do próprio TaxId.
    [Theory]
    [InlineData(CPF, "529.982.247-25")]
    [InlineData(CNPJ, "11.222.333/0001-81")]
    public void DisplayTaxId_WhenTheDocumentIsResolved_ShouldFormatIt(string raw, string expected)
    {
        var payer = MaskedParty.Of("Fulano", raw);

        Assert.Equal(expected, payer.DisplayTaxId);
    }

    // A máscara é pontuada no formato do tipo que o comprimento indica, com o oculto no lugar.
    [Theory]
    [InlineData("***.982.247-**", "***.982.247-**")]
    [InlineData("XXX982247XX", "***.982.247-**")]
    [InlineData("11.222.***/0001-**", "11.222.***/0001-**")]
    public void DisplayTaxId_WhenTheDocumentIsMasked_ShouldPunctuateTheMask(string raw, string expected)
    {
        var payer = MaskedParty.Of("Fulano", raw);

        Assert.Equal(expected, payer.DisplayTaxId);
    }

    // Inteiro mas com DV inválido não é identidade (ADR-011) — só é exibido, pontuado igual.
    [Fact]
    public void DisplayTaxId_WhenFullyVisibleButCheckDigitsFail_ShouldStillPunctuate()
    {
        var payer = MaskedParty.Of("Fulano", "52998224700");

        Assert.Null(payer.ResolvedTaxId);
        Assert.Equal("529.982.247-00", payer.DisplayTaxId);
    }

    // Comprimento que não é de CPF nem de CNPJ sai como veio: pontuar seria adivinhar o tipo.
    [Fact]
    public void DisplayTaxId_WhenTheLengthMatchesNoDocumentKind_ShouldReturnTheMaskAsIs()
    {
        var payer = MaskedParty.Of("Fulano", "***4567**");

        Assert.Equal("***4567**", payer.DisplayTaxId);
    }

    // Sem documento não há o que exibir.
    [Fact]
    public void DisplayTaxId_WithoutADocument_ShouldBeNull()
    {
        var payer = MaskedParty.Of("Fulano", maskedTaxId: null);

        Assert.Null(payer.DisplayTaxId);
    }

    // Conta os dígitos que o provedor deixou à mostra — é a medida de quanto a evidência vale.
    [Fact]
    public void VisibleDigitCount_ShouldCountOnlyTheDigitsLeftVisible()
    {
        var payer = MaskedParty.Of("Fulano", "***.982.247-**");

        Assert.Equal(6, payer.VisibleDigitCount);
    }
}
