namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.Services;

/// <summary>
/// O portão que decide se o corpo de uma mensagem vira artefato capturado.
/// </summary>
/// <remarks>
/// <para>
/// Sem portão, toda mensagem da caixa viraria <c>CaptureItem</c> e a fila de quarentena ficaria
/// inútil — é a mesma lição que fixou o descarte por desfecho na 2.3. Com portão errado, some a
/// conta que só chega por link, que é a maior parte das contas de concessionária.
/// </para>
/// <para>
/// <strong>Nenhum dos três sinais é palavra-chave.</strong> Um portão por assunto apagaria em
/// silêncio a cobrança cujo assunto é "Sua fatura chegou".
/// </para>
/// </remarks>
public class BodyCaptureGateServiceTests
{
    private const string PixPayloadInBody =
        "00020101021226770014BR.GOV.BCB.PIX2555api.exemplo/pix/qr/v2/00000000-0000-0000-0000-000000000000"
        + "5204000053039865802BR5906EXEMPLO6009SAO PAULO62070503***6304A76E";

    private static readonly string[] NoHosts = [];

    // BR Code escrito no corpo basta sozinho: é o formato novo da SABESP, e resolve sem abrir
    // arquivo e sem tocar a rede.
    [Fact]
    public void ShouldCapture_WhenBodyCarriesAPixPayload_ShouldCapture()
    {
        Assert.True(BodyCaptureGateService.ShouldCapture(
            $"Sua fatura chegou. {PixPayloadInBody}", links: null, NoHosts));
    }

    // Linha digitável escrita no corpo idem — é o formato antigo da SABESP, que manda o código de
    // barras de arrecadação no texto. A formatação com espaços é a que o emissor usa.
    [Fact]
    public void ShouldCapture_WhenBodyCarriesADigitableLine_ShouldCapture()
    {
        const string body = "Código de barras:\n82660000001 0 91320097091 5 11518797259 7 19554311753 3";

        Assert.True(BodyCaptureGateService.ShouldCapture(body, links: null, NoHosts));
    }

    // Link para host com receita configurada é o terceiro sinal — é o que traz o boleto do
    // condomínio e o da Perfil Líder, que não têm dígito nenhum no corpo.
    [Fact]
    public void ShouldCapture_WhenBodyLinksToAResolvableHost_ShouldCapture()
    {
        var links = new[] { DocumentLink.TryCreate("https://ssl.exemplo.com.br/Bill/abc")! };

        Assert.True(BodyCaptureGateService.ShouldCapture(
            "Já está disponível o boleto do mês.", links, ["ssl.exemplo.com.br"]));
    }

    // Link para host SEM receita não é sinal: o sistema não teria como buscar o documento, e o
    // item nasceria só para morrer na quarentena.
    [Fact]
    public void ShouldCapture_WhenBodyLinksToAnUnknownHost_ShouldNotCapture()
    {
        var links = new[] { DocumentLink.TryCreate("https://desconhecido.com.br/qualquer")! };

        Assert.False(BodyCaptureGateService.ShouldCapture(
            "Já está disponível o boleto do mês.", links, ["ssl.exemplo.com.br"]));
    }

    // Mensagem comum não vira item, mesmo falando de boleto: palavra no texto não é sinal.
    [Fact]
    public void ShouldCapture_WhenBodyIsOrdinaryConversation_ShouldNotCapture()
    {
        const string body = "Bom dia, poderia me enviar o boleto da última nota fiscal? Obrigado.";

        Assert.False(BodyCaptureGateService.ShouldCapture(body, links: null, NoHosts));
    }

    // Dígitos de linhas diferentes NÃO se emendam. Sem esta regra, uma lista de telefones ou de
    // protocolos viraria um candidato de 47 dígitos que não existe em lugar nenhum.
    [Fact]
    public void ShouldCapture_WhenLongDigitsSpanSeparateLines_ShouldNotCapture()
    {
        var body = string.Join('\n', Enumerable.Repeat("0800 721 0123", 10));

        Assert.False(BodyCaptureGateService.ShouldCapture(body, links: null, NoHosts));
    }

    // Corpo vazio, e ausência de links ou de receitas, não capturam nada.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldCapture_WithoutABody_ShouldNotCapture(string? body)
    {
        Assert.False(BodyCaptureGateService.ShouldCapture(body, links: null, resolvableHosts: null));
    }
}
