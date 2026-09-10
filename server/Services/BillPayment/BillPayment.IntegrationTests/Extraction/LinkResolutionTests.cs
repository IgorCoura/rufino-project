namespace BillPayment.IntegrationTests.Extraction;

using BillPayment.Infra.Extraction;
using BillPayment.Infra.Extraction.Links;
using Microsoft.Extensions.Options;

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
        Assert.Null(await Policy().ResolvePinnedAddressAsync(host, CancellationToken.None));
    }

    // Endereço público literal passa — a barreira recusa rede interna, não a internet.
    [Fact]
    public async Task IsPubliclyRoutable_WithAPublicAddress_ShouldAllow()
    {
        Assert.NotNull(await Policy().ResolvePinnedAddressAsync("8.8.8.8", CancellationToken.None));
    }

    // v4 mapeado em v6 é a forma clássica de contornar a checagem se ele não for desembrulhado.
    [Fact]
    public async Task IsPubliclyRoutable_WithAnIpv4MappedLoopback_ShouldRefuse()
    {
        Assert.Null(await Policy().ResolvePinnedAddressAsync(
            "::ffff:127.0.0.1", CancellationToken.None));
    }

    // TESTE-ÂNCORA do defeito de producao de 2026-09-10: o boleto da Acessorias chega dentro de um
    // `document.write("<iframe src=...>")` e a pagina inteira NAO tem uma unica ancora. A versao
    // que so lia `<a href>` chegava ali e voltava de maos vazias, e o item ia para a quarentena
    // como se o emissor nao tivesse documento.
    [Fact]
    public void Harvest_WithTheDocumentInsideAScriptWrittenIframe_ShouldFindIt()
    {
        const string html = """
            <html><body><script language="javascript">
            document.write("<iframe src='https://acessorias.s3.us-east-2.amazonaws.com/x/260902.pdf?X-Amz-Expires=120&X-Amz-Signature=abc' width='100%' />");
            </script></body></html>
            """;

        var links = HtmlLinkHarvester.HarvestFromPage(html);

        Assert.Contains(links, l => l.Host == "acessorias.s3.us-east-2.amazonaws.com"
            && l.PathAndQuery.Contains("260902.pdf", StringComparison.Ordinal));
    }

    // CONTRAPROVA do anterior: a entidade HTML tem que ser decodificada, senao a assinatura do S3
    // sai quebrada na URL e o balde responde 403 — que se pareceria com "documento nao existe".
    [Fact]
    public void Harvest_WithAnEncodedQueryString_ShouldDecodeTheAmpersands()
    {
        const string html = """<a href="https://emissor.com.br/b.pdf?a=1&amp;X-Amz-Signature=abc">Boleto</a>""";

        var link = Assert.Single(HtmlLinkHarvester.Harvest(html));

        Assert.Contains("a=1&x-amz-signature=abc", link.PathAndQuery, StringComparison.Ordinal);
    }

    // A ordem passou a decidir onde o orcamento e gasto: o e-mail da EDP tem oito rastreadores
    // antes da fatura, e gastar as requisicoes neles significa nao achar a fatura.
    [Fact]
    public void Harvest_ShouldRankWhatLooksLikeADocumentAheadOfTrackers()
    {
        const string html = """
            <a href="https://tracking.exemplo.com/abrir">Ver no navegador</a>
            <a href="https://facebook.com/emissor">Facebook</a>
            <a href="https://emissor.com.br/2via/boleto.pdf">Acessar Boleto</a>
            """;

        var links = HtmlLinkHarvester.Harvest(html);

        Assert.Equal("emissor.com.br", links[0].Host);
    }

    // Imagem em corpo de e-mail e pixel de rastreio: busca-la entrega ao remetente a confirmacao
    // de que a mensagem foi processada. Dentro de uma pagina ja buscada, porem, pode ser o boleto.
    [Fact]
    public void Harvest_ShouldIgnoreImagesInTheBodyButNotInAFetchedPage()
    {
        const string html = """<img src="https://rastreador.com.br/pixel.png" />""";

        // A promessa vale para AS DUAS passadas: a estruturada recusa a tag, e a bruta não pode
        // reencontrar o mesmo endereço no texto — foi exatamente esse o furo que este teste pegou.
        Assert.Empty(HtmlLinkHarvester.Harvest(html));
        Assert.Single(HtmlLinkHarvester.HarvestFromPage(html));
    }

    // Os tres buracos de IPv6 que a versao anterior deixava passar, mais as faixas nao-roteaveis
    // que faltavam. `::` e `::127.0.0.1` escapavam das tres checagens e alcancam loopback.
    [Theory]
    [InlineData("::")]                 // nao-especificado — chega no loopback na maioria dos stacks
    [InlineData("::127.0.0.1")]        // IPv4-compatible (RFC 4291, deprecado)
    [InlineData("64:ff9b::7f00:1")]    // NAT64
    [InlineData("2002:7f00:1::")]      // 6to4
    [InlineData("198.18.0.1")]         // benchmarking (RFC 2544)
    [InlineData("192.0.0.1")]          // atribuicoes do IETF
    [InlineData("203.0.113.10")]       // TEST-NET-3
    public async Task ResolvePinnedAddress_WithANonRoutableAddress_ShouldRefuse(string host)
    {
        Assert.Null(await Policy().ResolvePinnedAddressAsync(host, CancellationToken.None));
    }

    // A lista configuravel: e onde entram os enderecos publicos da propria instalacao, que o
    // codigo nao tem como adivinhar e que um atacante quer alcancar a partir de dentro.
    [Fact]
    public async Task ResolvePinnedAddress_WithAConfiguredBlockedRange_ShouldRefuseTheOwnPublicAddress()
    {
        var policy = Policy(blocked: ["8.8.8.0/24"]);

        Assert.Null(await policy.ResolvePinnedAddressAsync("8.8.8.8", CancellationToken.None));
        Assert.NotNull(await policy.ResolvePinnedAddressAsync("9.9.9.9", CancellationToken.None));
    }

    // A excecao vence a proibicao, e a ordem e deliberada: toda faixa que alguem precisa liberar
    // esta proibida por algum motivo, senao nao precisaria ser liberada.
    [Fact]
    public async Task ResolvePinnedAddress_WithAnAllowedRangeOverridingTheBlock_ShouldAllow()
    {
        var policy = Policy(blocked: ["8.8.8.0/24"], allowed: ["8.8.8.8"]);

        Assert.NotNull(await policy.ResolvePinnedAddressAsync("8.8.8.8", CancellationToken.None));
    }

    // Faixa malformada derruba o arranque em vez de ser ignorada: uma faixa que ninguem percebeu
    // que nao vale e pior que faixa nenhuma — quem a escreveu acredita estar protegido.
    [Fact]
    public void Policy_WithAMalformedRange_ShouldRefuseToStart()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Policy(blocked: ["10.0.0.0/99"]));

        Assert.Contains("BlockedCidrs", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A política sem faixa configurada — só as reservadas, que são código e não configuração.
    /// </summary>
    private static SafeUrlPolicy Policy(string[]? blocked = null, string[]? allowed = null)
        => new(Options.Create(new LinkResolutionOptions
        {
            BlockedCidrs = [.. blocked ?? []],
            AllowedCidrs = [.. allowed ?? []],
        }));
}
