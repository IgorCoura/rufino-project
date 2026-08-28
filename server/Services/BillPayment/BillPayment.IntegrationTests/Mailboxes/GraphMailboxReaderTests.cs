namespace BillPayment.IntegrationTests.Mailboxes;

using System.Net;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Mailboxes;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Mailboxes.Graph;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// A tradução entre o que o Microsoft Graph responde e o que o domínio entende.
/// </summary>
/// <remarks>
/// Sem rede e sem credencial: o que roda de verdade é o adapter; o que é substituído é o
/// transporte e o cofre. Uma caixa real exigiria um tenant Microsoft 365 na suíte.
/// </remarks>
public sealed class GraphMailboxReaderTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly CredentialRef Credential =
        CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c1"));

    private const string Mailbox = "contas@empresa.com.br";

    /// <summary>Forma real de um <c>deltaLink</c> do Graph — o caminho importa para o roteamento.</summary>
    private const string ExpiredDeltaLink =
        "https://graph.microsoft.com/v1.0/users/contas@empresa.com.br/mailFolders/inbox/messages/delta?$deltatoken=velho";
    private const string TokenBody = """{"access_token":"token-de-teste","expires_in":3600}""";
    private const string OneMessage = """
        {"value":[{"id":"msg-1","subject":"Sua fatura chegou","receivedDateTime":"2026-08-10T08:00:00Z",
        "hasAttachments":true,"from":{"emailAddress":{"address":"Faturas@ENEL.com.br"}}}],
        "@odata.deltaLink":"https://graph.microsoft.com/v1.0/delta?token=abc"}
        """;

    // Acesso concedido quando a caixa responde — é a prova exigida antes de a fonte existir.
    [Fact]
    public async Task ProbeAccess_WhenMailboxAnswers_ShouldGrant()
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages", HttpStatusCode.OK, """{"value":[]}"""));

        var probe = await reader.ProbeAccessAsync(Mailbox, Credential, folderPath: null, CancellationToken.None);

        Assert.True(probe.IsOk);
        Assert.Equal(Now, probe.AttemptedAt);
    }

    // 403 é a Application Access Policy barrando o app: NÃO é retentável, e insistir a cada
    // minuto esconderia um problema de configuração que só uma pessoa resolve.
    [Fact]
    public async Task ProbeAccess_WhenForbidden_ShouldDenyAndNotBeRetryable()
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages", HttpStatusCode.Forbidden,
                """{"error":{"code":"ErrorAccessDenied","message":"Access is denied."}}"""));

        var probe = await reader.ProbeAccessAsync(Mailbox, Credential, folderPath: null, CancellationToken.None);

        Assert.False(probe.IsOk);
        Assert.Same(MailboxStatus.Denied, probe.Status);
        Assert.False(probe.Status.IsRetryable);
        Assert.Equal("ErrorAccessDenied", probe.ReasonCode);
    }

    // Throttling é indisponibilidade: nada se aprendeu sobre a credencial, e esperar resolve.
    [Fact]
    public async Task ProbeAccess_WhenThrottled_ShouldBeUnavailableAndRetryable()
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages", HttpStatusCode.TooManyRequests, """{"error":{"code":"ActivityLimitReached"}}"""));

        var probe = await reader.ProbeAccessAsync(Mailbox, Credential, folderPath: null, CancellationToken.None);

        Assert.Same(MailboxStatus.Unavailable, probe.Status);
        Assert.True(probe.Status.IsRetryable);
    }

    // Credencial que não é o JSON esperado é recusada sem sequer pedir token.
    [Fact]
    public async Task ProbeAccess_WithMalformedCredential_ShouldDenyWithoutCallingTheProvider()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("oauth2", HttpStatusCode.OK, TokenBody);
        var reader = Build(handler, storedCredential: "isto-nao-e-json");

        var probe = await reader.ProbeAccessAsync(Mailbox, Credential, folderPath: null, CancellationToken.None);

        Assert.Same(MailboxStatus.Denied, probe.Status);
        Assert.Equal("credential_malformed", probe.ReasonCode);
        Assert.Empty(handler.Requests);
    }

    // A varredura converte mensagem e anexos, normaliza o remetente e guarda o deltaLink.
    [Fact]
    public async Task Read_ShouldMapMessagesAndAttachments()
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages/delta", HttpStatusCode.OK, OneMessage)
            .Route("/attachments", HttpStatusCode.OK, """
                {"value":[{"id":"att-1","name":"boleto.pdf","contentType":"application/pdf","size":51200,"isInline":false}]}
                """));

        var result = await reader.ReadAsync(Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        Assert.True(result.IsOk);
        Assert.Equal("https://graph.microsoft.com/v1.0/delta?token=abc", result.NextCursor);

        var message = Assert.Single(result.Messages);
        Assert.Equal("msg-1", message.MessageId);
        Assert.Equal("faturas@enel.com.br", message.Sender);

        var artifact = Assert.Single(message.Artifacts);
        Assert.Equal("att-1", artifact.Key);
        Assert.Equal("boleto.pdf", artifact.FileName);
    }

    // Assinatura embutida, arquivo grande demais e tipo fora da lista não viram artefato —
    // sem esses filtros, toda mensagem com logotipo entraria como boleto candidato.
    [Fact]
    public async Task Read_ShouldFilterInlineOversizedAndDisallowedAttachments()
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages/delta", HttpStatusCode.OK, OneMessage)
            .Route("/attachments", HttpStatusCode.OK, """
                {"value":[
                  {"id":"logo","name":"logo.png","contentType":"image/png","size":2048,"isInline":true},
                  {"id":"video","name":"institucional.pdf","contentType":"application/pdf","size":99999999,"isInline":false},
                  {"id":"convite","name":"reuniao.ics","contentType":"text/calendar","size":1024,"isInline":false},
                  {"id":"boleto","name":"boleto.pdf","contentType":"application/pdf; charset=binary","size":51200,"isInline":false}
                ]}
                """));

        var result = await reader.ReadAsync(Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        var artifact = Assert.Single(Assert.Single(result.Messages).Artifacts);
        Assert.Equal("boleto", artifact.Key);
    }

    // Mensagem removida da pasta desde o último cursor é ignorada — o @removed não desfaz o que
    // já foi ingerido, e reprocessá-la não faria sentido. A mensagem SEM anexo, ao contrário,
    // continua subindo desde 2026-08-26: ela não vira item, mas precisa chegar ao livro-caixa.
    [Fact]
    public async Task Read_ShouldSkipRemovedButKeepAttachmentlessMessages()
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages/delta", HttpStatusCode.OK, """
                {"value":[
                  {"id":"apagada","@removed":{"reason":"deleted"}},
                  {"id":"sem-anexo","hasAttachments":false,"receivedDateTime":"2026-08-10T08:00:00Z",
                   "from":{"emailAddress":{"address":"news@fornecedor.com"}}}
                ],"@odata.deltaLink":"https://graph.microsoft.com/v1.0/delta?token=abc"}
                """));

        var result = await reader.ReadAsync(
            Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        Assert.True(result.IsOk);

        var message = Assert.Single(result.Messages);
        Assert.Equal("sem-anexo", message.MessageId);
        Assert.Empty(message.Artifacts);
    }

    // A varredura segue o nextLink até a última página, e só ali existe cursor a guardar.
    [Fact]
    public async Task Read_ShouldFollowNextLinkUntilDeltaLinkAppears()
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .RouteSequence(
                "/messages/delta",
                """{"value":[],"@odata.nextLink":"https://graph.microsoft.com/v1.0/me/messages/delta?$skiptoken=p2"}""",
                """{"value":[],"@odata.deltaLink":"https://graph.microsoft.com/v1.0/delta?token=final"}"""));

        var result = await reader.ReadAsync(Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        Assert.True(result.IsOk);
        Assert.Equal("https://graph.microsoft.com/v1.0/delta?token=final", result.NextCursor);
    }

    // Ao bater o teto de páginas, o nextLink vira o cursor: a varredura seguinte RETOMA de onde
    // parou, em vez de recomeçar a caixa inteira toda vez.
    [Fact]
    public async Task Read_WhenPageCapIsReached_ShouldKeepNextLinkAsCursor()
    {
        var semFim = """{"value":[],"@odata.nextLink":"https://graph.microsoft.com/v1.0/me/messages/delta?$skiptoken=p9"}""";

        var reader = Build(
            new RoutingStubHttpMessageHandler()
                .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
                .Route("/messages/delta", HttpStatusCode.OK, semFim),
            maxPages: 3);

        var result = await reader.ReadAsync(Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        Assert.True(result.IsOk);
        Assert.Equal("https://graph.microsoft.com/v1.0/me/messages/delta?$skiptoken=p9", result.NextCursor);
    }

    // 410 é o Graph invalidando o deltaLink velho: vira CursorExpired, que pede varredura
    // completa — e não Unavailable, que só mandaria tentar de novo com o mesmo cursor morto.
    [Fact]
    public async Task Read_WhenDeltaTokenExpired_ShouldReturnCursorExpired()
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages/delta", HttpStatusCode.Gone,
                """{"error":{"code":"resyncRequired","message":"Resync required."}}"""));

        var result = await reader.ReadAsync(Mailbox, Credential, folderPath: null, ExpiredDeltaLink, capturedSince: null, CancellationToken.None);

        Assert.Same(MailboxStatus.CursorExpired, result.Status);
        Assert.True(result.RequiresCursorReset);
        Assert.Equal("resyncRequired", result.ReasonCode);
    }

    // Teste de regressão: um cursor corrompido no banco chegava cru ao HttpClient e estourava
    // InvalidOperationException, que não é falha de transporte — a exceção escapava do adapter e
    // derrubava a varredura em vez de ser registrada. Agora vira CursorExpired, cuja recuperação
    // já é a certa: descartar o cursor e varrer a caixa inteira.
    [Theory]
    [InlineData("cursor-velho")]
    [InlineData("/relativo/delta")]
    [InlineData("ftp://graph.microsoft.com/v1.0/delta")]
    public async Task Read_WithMalformedCursor_ShouldReturnCursorExpiredInsteadOfThrowing(string cursor)
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody));

        var result = await reader.ReadAsync(Mailbox, Credential, folderPath: null, cursor, capturedSince: null, CancellationToken.None);

        Assert.Same(MailboxStatus.CursorExpired, result.Status);
        Assert.Equal("cursor_malformed", result.ReasonCode);
    }

    // O cursor recebido é usado como URL da varredura — sem isso a leitura não seria incremental.
    [Fact]
    public async Task Read_WithCursor_ShouldResumeFromIt()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/v1.0/delta", HttpStatusCode.OK,
                """{"value":[],"@odata.deltaLink":"https://graph.microsoft.com/v1.0/delta?token=novo"}""");

        var reader = Build(handler);

        await reader.ReadAsync(Mailbox, Credential, folderPath: null, "https://graph.microsoft.com/v1.0/delta?token=abc", capturedSince: null, CancellationToken.None);

        Assert.Contains(handler.Requests, uri => uri.Query.Contains("token=abc", StringComparison.Ordinal));
    }

    // O token é reaproveitado entre varreduras — pedir um por varredura faria o Entra ID
    // limitar a taxa da própria autenticação.
    [Fact]
    public async Task Read_Twice_ShouldRequestTokenOnlyOnce()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages/delta", HttpStatusCode.OK, """{"value":[],"@odata.deltaLink":"d"}""");

        var reader = Build(handler);

        await reader.ReadAsync(Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);
        await reader.ReadAsync(Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        var pedidosDeToken = handler.Requests.Count(uri => uri.ToString().Contains("oauth2", StringComparison.Ordinal));
        Assert.Equal(1, pedidosDeToken);
    }

    // Segredo recusado pelo Entra ID é Denied: esperar não conserta um app mal configurado.
    [Fact]
    public async Task Read_WhenTokenIsRejected_ShouldDeny()
    {
        var reader = Build(new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.Unauthorized, """{"error":"invalid_client"}"""));

        var result = await reader.ReadAsync(Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        Assert.Same(MailboxStatus.Denied, result.Status);
        Assert.Equal("token_request_failed", result.ReasonCode);
    }

    private static GraphMailboxReader Build(
        RoutingStubHttpMessageHandler handler,
        string? storedCredential = null,
        int maxPages = 20)
    {
        var options = Options.Create(new GraphOptions
        {
            Enabled = true,
            MaxPagesPerSync = maxPages,
            MaxAttachmentBytes = 20 * 1024 * 1024,
        });

        var clock = new FixedTimeProvider(Now);
        var factory = new StubHttpClientFactory(handler);

        var vault = new StubSecretVault(storedCredential
            ?? """{"directoryId":"11111111-1111-1111-1111-111111111111","clientId":"22222222-2222-2222-2222-222222222222","clientSecret":"s3cr3t"}""");

        var tokenProvider = new GraphTokenProvider(
            factory, options, clock, NullLogger<GraphTokenProvider>.Instance);

        // Sem receita de link: o portão do corpo passa a depender só do que estiver escrito no
        // texto, que é o que estes testes exercitam.
        return new GraphMailboxReader(
            factory,
            vault,
            tokenProvider,
            new NoRecipeLinkResolver(),
            options,
            clock,
            NullLogger<GraphMailboxReader>.Instance);
    }

    /// <summary>Resolvedor sem receita nenhuma — nenhum host é buscável.</summary>
    private sealed class NoRecipeLinkResolver : IDocumentLinkResolver
    {
        public bool IsEnabled => false;

        public IReadOnlyCollection<string> ResolvableHosts => [];

        public Task<ResolvedDocument?> ResolveAsync(
            ReadOnlyMemory<byte> body,
            string? contentType,
            CancellationToken cancellationToken)
            => Task.FromResult<ResolvedDocument?>(null);

        public IReadOnlyCollection<DocumentLink> HarvestLinks(ReadOnlyMemory<byte> body, string? contentType) => [];
    }

    /// <summary>Cofre de teste: devolve o que foi programado. O cofre real tem suíte própria.</summary>
    private sealed class StubSecretVault(string secret) : ISecretVault
    {
        public Task<string> ResolveAsync(CredentialRef credentialRef, CancellationToken cancellationToken)
            => Task.FromResult(secret);

        public Task<CredentialRef> StoreAsync(
            TenantId tenantId, SecretKind kind, string value, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task ReplaceAsync(CredentialRef credentialRef, string value, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task RemoveAsync(CredentialRef credentialRef, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    // O piso temporal vira o único $filter que a delta query aceita — receivedDateTime ge {data}.
    [Fact]
    public async Task Read_WithCaptureSince_ShouldSendTheReceivedDateTimeFloorFilter()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages/delta", HttpStatusCode.OK, """{"value":[],"@odata.deltaLink":"d"}""");

        var reader = Build(handler);

        await reader.ReadAsync(
            Mailbox, Credential, folderPath: null, cursor: null,
            capturedSince: new DateOnly(2026, 5, 27), CancellationToken.None);

        var delta = Assert.Single(handler.Requests, uri => uri.AbsolutePath.Contains("/messages/delta", StringComparison.Ordinal));
        Assert.Contains("receivedDateTime ge 2026-05-27T00:00:00Z", Uri.UnescapeDataString(delta.Query), StringComparison.Ordinal);
    }

    // Sem piso a URL não carrega filtro nenhum — é o comportamento de sempre, preservado.
    [Fact]
    public async Task Read_WithoutCaptureSince_ShouldNotSendAnyFilter()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages/delta", HttpStatusCode.OK, """{"value":[],"@odata.deltaLink":"d"}""");

        var reader = Build(handler);

        await reader.ReadAsync(Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        var delta = Assert.Single(handler.Requests, uri => uri.AbsolutePath.Contains("/messages/delta", StringComparison.Ordinal));
        Assert.DoesNotContain("$filter", delta.Query, StringComparison.Ordinal);
    }

    // CONTRAPROVA: havendo cursor, o piso NÃO é reacrescentado — o provedor já gravou o filtro
    // dentro do deltaLink, e repeti-lo produziria uma URL com o parâmetro duas vezes.
    [Fact]
    public async Task Read_WithCursor_ShouldNotReapplyTheFloorFilter()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/v1.0/delta", HttpStatusCode.OK, """{"value":[],"@odata.deltaLink":"d"}""");

        var reader = Build(handler);

        await reader.ReadAsync(
            Mailbox, Credential, folderPath: null,
            cursor: "https://graph.microsoft.com/v1.0/delta?token=abc",
            capturedSince: new DateOnly(2026, 5, 27), CancellationToken.None);

        var delta = Assert.Single(handler.Requests, uri => uri.AbsolutePath.Contains("/v1.0/delta", StringComparison.Ordinal));
        Assert.Equal("?token=abc", delta.Query);
    }

    // REGRESSÃO (2026-08-26): mensagem SEM anexo e sem sinal de cobrança no corpo era descartada
    // dentro do adaptador — sumia do livro-caixa, que existe justamente para responder "o que
    // houve com o e-mail que eu mandei". Três e-mails reais na caixa de entrada ficaram
    // invisíveis, um deles com assunto "uma cobrança foi gerada para você".
    [Fact]
    public async Task Read_WhenAMessageHasNoAttachmentAndNoPayableBody_ShouldStillReturnIt()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages/delta", HttpStatusCode.OK, """
                {"value":[{"id":"msg-sem-anexo","subject":"SECONCI - PENDENCIA",
                           "receivedDateTime":"2026-08-26T18:00:41Z","hasAttachments":false,
                           "from":{"emailAddress":{"address":"aviso@seconci-sp.org.br"}},
                           "body":{"contentType":"text","content":"Prezado, regularize sua pendencia."}}],
                 "@odata.deltaLink":"https://graph.microsoft.com/v1.0/delta?$deltatoken=fim"}
                """);

        var result = await Build(handler).ReadAsync(
            Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        Assert.True(result.IsOk);

        var message = Assert.Single(result.Messages);
        Assert.Equal("msg-sem-anexo", message.MessageId);
        Assert.Empty(message.Artifacts);
    }

    // REGRESSÃO: parar no teto de páginas tem de ser distinguível de chegar ao fim da caixa. A
    // enumeração do provedor vai do mais antigo para o mais novo, então uma varredura truncada
    // deixa a mensagem recém-chegada fora de alcance até o ciclo seguinte.
    [Fact]
    public async Task Read_WhenTheSweepStopsAtThePageCap_ShouldReportMorePages()
    {
        // Só nextLink, nunca deltaLink: o teto de páginas é alcançado antes do fim.
        var handler = new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("delta", HttpStatusCode.OK, """
                {"value":[],"@odata.nextLink":"https://graph.microsoft.com/v1.0/delta?$skiptoken=mais"}
                """);

        var result = await Build(handler).ReadAsync(
            Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        Assert.True(result.IsOk);
        Assert.True(result.HasMorePages);
    }

    // CONTRAPROVA: chegando o deltaLink, a varredura acabou e o agendador pode dormir o intervalo
    // normal. Sem esta, o sinal poderia estar sempre ligado e o worker viraria laço apertado.
    [Fact]
    public async Task Read_WhenTheSweepReachesTheDeltaLink_ShouldNotReportMorePages()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("oauth2/v2.0/token", HttpStatusCode.OK, TokenBody)
            .Route("/messages/delta", HttpStatusCode.OK, """{"value":[],"@odata.deltaLink":"d"}""");

        var result = await Build(handler).ReadAsync(
            Mailbox, Credential, folderPath: null, cursor: null, capturedSince: null, CancellationToken.None);

        Assert.False(result.HasMorePages);
    }
}
