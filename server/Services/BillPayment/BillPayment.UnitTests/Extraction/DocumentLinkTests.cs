namespace BillPayment.UnitTests.Extraction;

using BillPayment.Domain.Extraction;

/// <summary>
/// O link já reduzido ao endereço que seria de fato visitado.
/// </summary>
public class DocumentLinkTests
{
    // A porta faz parte da identidade: o PDF da SABESP vive em :7446, e uma regra que assumisse
    // 443 perderia o único documento hoje alcançável por download direto.
    [Fact]
    public void TryCreate_WithNonDefaultPort_ShouldKeepIt()
    {
        var link = DocumentLink.TryCreate("https://file-pdf.exemplo.com.br:7446/dx/abc.pdf");

        Assert.NotNull(link);
        Assert.Equal(7446, link.Port);
        Assert.Equal("file-pdf.exemplo.com.br", link.Host);
    }

    // Host e caminho ficam em minúsculas para a receita casar sem depender de como o emissor
    // escreveu a URL no HTML.
    [Fact]
    public void TryCreate_ShouldNormalizeHostAndPathForMatching()
    {
        var link = DocumentLink.TryCreate("https://SSL.Exemplo.COM.br/Bill/ABC-123");

        Assert.NotNull(link);
        Assert.Equal("ssl.exemplo.com.br", link.Host);
        Assert.StartsWith("/bill/", link.PathAndQuery, StringComparison.Ordinal);
    }

    // O que não é http(s) absoluto não vira link — e recusar é o caso COMUM, não a exceção: um
    // e-mail traz dezenas de href, a maioria mailto, tel, âncora ou lixo de template.
    [Theory]
    [InlineData("mailto:contato@exemplo.com.br")]
    [InlineData("tel:08006030023")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("#")]
    [InlineData("/relativo/boleto")]
    [InlineData("")]
    [InlineData(null)]
    public void TryCreate_WithUnusableAddress_ShouldReturnNull(string? url)
    {
        Assert.Null(DocumentLink.TryCreate(url));
    }

    // Endereço maior que o teto é recusado em vez de truncado: URL cortada apontaria para outro
    // lugar, e buscar "outro lugar" é exatamente o que a escada existe para impedir.
    [Fact]
    public void TryCreate_WhenUrlExceedsTheLimit_ShouldReturnNull()
    {
        var huge = "https://exemplo.com.br/" + new string('a', DocumentLink.URL_MAX_LENGTH);

        Assert.Null(DocumentLink.TryCreate(huge));
    }

    // O texto da âncora é sinal barato para distinguir "Acessar Boleto" de um ícone de rede
    // social, mas não pode crescer sem limite.
    [Fact]
    public void TryCreate_WithAnOversizedLabel_ShouldClampIt()
    {
        var link = DocumentLink.TryCreate(
            "https://exemplo.com.br/boleto", new string('x', DocumentLink.LABEL_MAX_LENGTH + 50));

        Assert.NotNull(link);
        Assert.Equal(DocumentLink.LABEL_MAX_LENGTH, link.Label!.Length);
    }

    // Igualdade é pelo endereço final: o mesmo boleto apontado por dois rastreadores diferentes
    // é um link só, e buscá-lo duas vezes gastaria requisição do teto por mensagem.
    [Fact]
    public void Equality_ShouldBeDrivenByTheFinalAddressOnly()
    {
        var first = DocumentLink.TryCreate("https://exemplo.com.br/boleto", "Acessar Boleto", wasWrapped: true);
        var second = DocumentLink.TryCreate("https://exemplo.com.br/boleto", "Abrir fatura", wasWrapped: false);

        Assert.Equal(first, second);
    }
}
