namespace BillPayment.IntegrationTests.Extraction;

using BillPayment.Infra.Extraction;
using BillPayment.Infra.Extraction.Links;

/// <summary>
/// As duas peças que decidem o que a escada de link vai buscar: a colheita e a barreira de rede.
/// </summary>
/// <remarks>
/// Sem rede: o que roda aqui é a seleção do endereço, que é onde os erros custam caro. A busca em
/// si tem substituto na suíte, porque um teste que fizesse requisição de verdade mediria se o
/// servidor do emissor está no ar.
/// </remarks>
public sealed class LinkResolutionTests
{
    /// <summary>
    /// O e-mail do condomínio, na forma medida: o botão do boleto e, logo abaixo, um link de
    /// propaganda no MESMO host.
    /// </summary>
    private const string CondoEmail = """
        <html><body>
          <a href="https://ssl.exemplo.com.br">logotipo</a>
          <a href="https://ssl.exemplo.com.br/Bill/8a467507-e583-44e6-b2ee-62207d1c0438">Acessar Boleto</a>
          <a href="https://ssl.exemplo.com.br/EmailAdvertisingClick/Index?q=7e94834d&amp;u=248625">&nbsp;</a>
          <a href="tel:08006030023">0800 603 0023</a>
          <a href="mailto:contato@exemplo.com.br">contato@exemplo.com.br</a>
        </body></html>
        """;

    // "Pegue o primeiro link" erra em todos os casos medidos: aqui o primeiro é o logotipo e o
    // terceiro é propaganda. A colheita devolve todos, com o texto da âncora, e quem escolhe é a
    // receita — nunca a ordem.
    [Fact]
    public void Harvest_WithDecoyLinks_ShouldReturnThemAllWithTheirLabels()
    {
        var links = HtmlLinkHarvester.Harvest(CondoEmail);

        var boleto = Assert.Single(links, l => l.PathAndQuery.StartsWith("/bill/", StringComparison.Ordinal));
        Assert.Equal("Acessar Boleto", boleto.Label);

        // mailto e tel não são endereços buscáveis e não entram.
        Assert.All(links, l => Assert.Equal("ssl.exemplo.com.br", l.Host));
        Assert.Equal(3, links.Count);
    }

    // O rastreador de campanha é desfeito na colheita, sem nenhuma chamada de rede: a allowlist
    // precisa decidir sobre o destino, não sobre o domínio de quem rastreia.
    [Fact]
    public void Harvest_WithATrackedLink_ShouldYieldTheUnwrappedTarget()
    {
        const string html = """
            <a href="https://abc.r.us-east-1.awstrack.me/L0/https:%2F%2Ffile-pdf.exemplo.com.br:7446%2Fdx%2Fa.pdf/1/x/y=1">
            Abrir fatura</a>
            """;

        var link = Assert.Single(HtmlLinkHarvester.Harvest(html));

        Assert.True(link.WasWrapped);
        Assert.Equal("file-pdf.exemplo.com.br", link.Host);
        Assert.Equal(7446, link.Port);
    }

    // O mesmo boleto apontado por dois rastreadores diferentes é um link só — buscá-lo duas vezes
    // gastaria o teto de requisições por mensagem à toa.
    [Fact]
    public void Harvest_WithTheSameTargetWrappedTwice_ShouldDeduplicate()
    {
        const string html = """
            <a href="https://a.exemplo.com/r?url=https%3A%2F%2Fdestino.com.br%2Fboleto">um</a>
            <a href="https://b.exemplo.com/r?u=https%3A%2F%2Fdestino.com.br%2Fboleto">dois</a>
            """;

        Assert.Single(HtmlLinkHarvester.Harvest(html));
    }

    // Corpo sem link nenhum não produz candidato — e não pode explodir.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bom dia, segue em anexo a documentação.")]
    public void Harvest_WithoutAnyAnchor_ShouldReturnEmpty(string html)
    {
        Assert.Empty(HtmlLinkHarvester.Harvest(html));
    }

    // A allowlist decide sobre o NOME, e o nome é resolvido por um DNS que não é nosso. Um host
    // autorizado que passe a apontar para dentro da rede transformaria a escada num canal para
    // alcançar serviço interno e metadado de nuvem a partir de um e-mail.
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.30.1")]
    [InlineData("192.168.15.20")]
    [InlineData("169.254.169.254")] // endereço de metadados de nuvem
    [InlineData("100.64.0.1")]      // CGNAT
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    [InlineData("fd00::1")]
    [InlineData("")]
    public async Task IsPubliclyRoutable_WithAnInternalAddress_ShouldRefuse(string host)
    {
        Assert.False(await SafeUrlPolicy.IsPubliclyRoutableAsync(host, CancellationToken.None));
    }

    // Endereço público literal passa — a barreira recusa rede interna, não a internet.
    [Fact]
    public async Task IsPubliclyRoutable_WithAPublicAddress_ShouldAllow()
    {
        Assert.True(await SafeUrlPolicy.IsPubliclyRoutableAsync("8.8.8.8", CancellationToken.None));
    }

    // v4 mapeado em v6 é a forma clássica de contornar a checagem se ele não for desembrulhado.
    [Fact]
    public async Task IsPubliclyRoutable_WithAnIpv4MappedLoopback_ShouldRefuse()
    {
        Assert.False(await SafeUrlPolicy.IsPubliclyRoutableAsync(
            "::ffff:127.0.0.1", CancellationToken.None));
    }
}
