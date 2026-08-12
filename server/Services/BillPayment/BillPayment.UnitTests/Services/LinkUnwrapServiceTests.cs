namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Services;

/// <summary>
/// O desembrulho de rastreador de campanha.
/// </summary>
/// <remarks>
/// <para>
/// A regra que estes testes protegem é de <strong>segurança, não de estética</strong>: a allowlist
/// da escada de link decide sobre o host, e todo boleto por link medido na caixa real chega dentro
/// de um rastreador. Autorizar o host do rastreador seria autorizar redirecionamento para qualquer
/// lugar; recusá-lo sem desembrulhar perderia todos os boletos por link que existem.
/// </para>
/// <para>
/// Os endereços aqui são os <strong>formatos</strong> medidos em 2026-08-11 — com identificadores
/// trocados, porque URL de boleto é credencial ao portador e não entra em repositório.
/// </para>
/// </remarks>
public class LinkUnwrapServiceTests
{
    // O rastreador do Amazon SES esconde o destino como um segmento percent-encoded do caminho.
    // É a forma que a SABESP e a Perfil Líder usam.
    [Fact]
    public void Unwrap_WithSesTracker_ShouldReturnTheRealTarget()
    {
        const string wrapped =
            "https://vjmh2gkk.r.us-east-1.awstrack.me/L0/"
            + "https:%2F%2Fexemplo.com.br%2FdirectScript%3Fhash=00000000000000000000000000000000"
            + "/1/0100000000000000-0000/aaaa=473";

        var (url, wasWrapped) = LinkUnwrapService.Unwrap(wrapped);

        Assert.True(wasWrapped);
        Assert.Equal("https://exemplo.com.br/directScript?hash=00000000000000000000000000000000", url);
    }

    // Porta não-padrão sobrevive ao desembrulho: o PDF da SABESP vive em :7446, e perder a porta
    // faria a receita nunca casar.
    [Fact]
    public void Unwrap_WhenTargetHasNonDefaultPort_ShouldPreserveIt()
    {
        const string wrapped =
            "https://crn0hnc3.r.sa-east-1.awstrack.me/L0/"
            + "https:%2F%2Ffile-pdf.exemplo.com.br:7446%2Fdx%2Fabc.pdf/2/000/bbb=258";

        var (url, _) = LinkUnwrapService.Unwrap(wrapped);

        Assert.Equal("https://file-pdf.exemplo.com.br:7446/dx/abc.pdf", url);
    }

    // Rastreador por query string também é desfeito — a forma varia por plataforma de envio.
    [Theory]
    [InlineData("https://click.exemplo.com/r?url=https%3A%2F%2Fdestino.com.br%2Fboleto")]
    [InlineData("https://click.exemplo.com/r?u=https%3A%2F%2Fdestino.com.br%2Fboleto&x=1")]
    [InlineData("https://click.exemplo.com/r?a=1&redirect=https%3A%2F%2Fdestino.com.br%2Fboleto")]
    public void Unwrap_WithQueryStringTracker_ShouldReturnTheRealTarget(string wrapped)
    {
        var (url, wasWrapped) = LinkUnwrapService.Unwrap(wrapped);

        Assert.True(wasWrapped);
        Assert.Equal("https://destino.com.br/boleto", url);
    }

    // Rastreador opaco continua como está — é o caso da EDP, cujo ?ref= é um identificador
    // interno e não carrega URL nenhuma. Não há o que decodificar, e o link segue apontando para
    // o rastreador, onde a allowlist o recusa. Esse É o desfecho correto.
    [Fact]
    public void Unwrap_WithOpaqueTracker_ShouldLeaveTheAddressUntouched()
    {
        const string opaque = "https://tracking.exemplo.com.br/?ref=0ygAAOeRVW6dHMJVloLI14htDhDe1zBWAQAAAK4J0Ypi";

        var (url, wasWrapped) = LinkUnwrapService.Unwrap(opaque);

        Assert.False(wasWrapped);
        Assert.Equal(opaque, url);
    }

    // Embrulho dentro de embrulho acontece quando o e-mail é reencaminhado por outra plataforma.
    [Fact]
    public void Unwrap_WithNestedTrackers_ShouldPeelUntilTheRealTarget()
    {
        const string inner = "https%3A%2F%2Fdestino.com.br%2Fboleto";
        var wrapped = $"https://a.exemplo.com/r?url=https%3A%2F%2Fb.exemplo.com%2Fr%3Furl%3D{inner}";

        var (url, _) = LinkUnwrapService.Unwrap(wrapped);

        Assert.Equal("https://destino.com.br/boleto", url);
    }

    // Link comum não é tocado, e entrada que não é URL absoluta volta como veio — quem recusa é
    // o DocumentLink, num lugar só.
    [Theory]
    [InlineData("https://exemplo.com.br/boleto/123")]
    [InlineData("mailto:contato@exemplo.com.br")]
    [InlineData("#")]
    [InlineData("")]
    public void Unwrap_WithoutAnyWrapping_ShouldReturnTheInput(string input)
    {
        var (url, wasWrapped) = LinkUnwrapService.Unwrap(input);

        Assert.False(wasWrapped);
        Assert.Equal(input, url);
    }

    // Um destino embutido que NÃO é http não pode virar alvo: file:// e javascript: dentro do
    // parâmetro seriam a porta dos fundos para sair do protocolo.
    [Theory]
    [InlineData("https://click.exemplo.com/r?url=file%3A%2F%2F%2Fetc%2Fpasswd")]
    [InlineData("https://click.exemplo.com/r?url=javascript%3Aalert(1)")]
    public void Unwrap_WhenEmbeddedTargetIsNotHttp_ShouldRefuseToUnwrap(string wrapped)
    {
        var (url, wasWrapped) = LinkUnwrapService.Unwrap(wrapped);

        Assert.False(wasWrapped);
        Assert.Equal(wrapped, url);
    }
}
