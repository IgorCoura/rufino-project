namespace PeopleManagement.IntegrationTests.Tests.Authorization;

using System.Net;
using System.Text;
using PeopleManagement.API.Authorization;
using AuthorizationOptions = PeopleManagement.API.Authorization.AuthorizationOptions;

/// <summary>
/// Tradução da resposta UMA do Keycloak para <see cref="RptFetchResult"/>, e a resolução da
/// permissão contra o retrato.
/// </summary>
/// <remarks>
/// <para>
/// Sem containers e sem rede: o que se mede é a leitura da resposta do provedor, e um teste que
/// dependesse de um Keycloak no ar mediria se ele está no ar. Mesma doutrina dos adapters do
/// ZapSign e do WhatsApp, que já são exercitados por <c>StubHttpMessageHandler</c>.
/// </para>
/// <para>
/// Reescrito em 2026-09-04, quando o cliente passou a buscar o retrato INTEIRO de uma vez (uma ida
/// por token, não por endpoint). As três distinções que sustentam o status HTTP continuam as
/// mesmas — token recusado, indisponibilidade e negativa —, mas a negativa deixou de ser um
/// desfecho do cliente e passou a ser um retrato que não concede.
/// </para>
/// </remarks>
public sealed class AuthorizationServerClientTests
{
    private static AuthorizationServerClient CreateClient(HttpMessageHandler handler)
    {
        var options = new AuthorizationOptions
        {
            AuthServerUrl = "http://keycloak.test",
            Realm = "rufino",
            Resource = "people-management-api",
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.KeycloakUrlRealm),
        };

        return new AuthorizationServerClient(httpClient, options);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("connection refused");
    }

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string body = "{}")
        => new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static bool Grants(RptFetchResult result, string permission)
        => result.Snapshot.Grants(permission, ScopesValidationMode.AllOf);

    // 401 do Keycloak é o TOKEN recusado, não permissão faltando — e sai como InvalidToken para
    // virar 401 lá na frente, em vez de 403.
    [Fact]
    public async Task Fetch_WhenKeycloakRejectsTheToken_ShouldReturnInvalidToken()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.Unauthorized,
            "{\"error\":\"invalid_grant\",\"error_description\":\"Invalid bearer token\"}")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.Equal(RptFetchOutcome.InvalidToken, result.Outcome);
    }

    // 403 access_denied é "esta pessoa não alcança nada": retrato VAZIO, não indisponibilidade.
    // Tratá-lo como falha faria quem não tem permissão receber 503 — e, com fail-static, herdar o
    // retrato guardado de uma sessão anterior.
    [Fact]
    public async Task Fetch_WhenKeycloakAnswersAccessDenied_ShouldResolveToAnEmptySnapshot()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.Forbidden,
            "{\"error\":\"access_denied\"}")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.Equal(RptFetchOutcome.Resolved, result.Outcome);
        Assert.False(Grants(result, "document#approve"));
    }

    // 400 também é retrato vazio: pedido malformado não concede nada, e não é indisponibilidade.
    [Fact]
    public async Task Fetch_WhenKeycloakAnswersBadRequest_ShouldResolveToAnEmptySnapshot()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.BadRequest,
            "{\"error\":\"invalid_request\"}")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.Equal(RptFetchOutcome.Resolved, result.Outcome);
        Assert.False(Grants(result, "document#approve"));
    }

    // 5xx é indisponibilidade, e precisa se distinguir de negativa para virar 503.
    [Fact]
    public async Task Fetch_WhenKeycloakAnswersAServerError_ShouldReturnUnavailable()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.BadGateway, "")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.Equal(RptFetchOutcome.Unavailable, result.Outcome);
    }

    // Falha de rede é indisponibilidade, não exceção subindo pelo middleware como 500.
    [Fact]
    public async Task Fetch_WhenTheRequestFailsAtTheNetworkLevel_ShouldReturnUnavailable()
    {
        var client = CreateClient(new ThrowingHandler());

        var result = await client.FetchAllPermissionsAsync();

        Assert.Equal(RptFetchOutcome.Unavailable, result.Outcome);
    }

    // Corpo que não é a lista esperada num 200 é indisponibilidade, NUNCA retrato vazio: o
    // provedor mudou de contrato, e negar todo mundo em silêncio seria a pior leitura possível.
    [Fact]
    public async Task Fetch_WhenTheBodyIsNotTheExpectedList_ShouldReturnUnavailable()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK, "{\"result\":true}")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.Equal(RptFetchOutcome.Unavailable, result.Outcome);
    }

    // O pedido NÃO leva o parâmetro 'permission': é a ausência dele que faz o Keycloak avaliar
    // tudo e devolver a lista inteira. Mandá-lo de volta faria o cache guardar o retrato de UMA
    // permissão e negar todas as outras.
    [Fact]
    public async Task Fetch_ShouldAskForEveryPermission_NotASpecificOne()
    {
        string? body = null;
        var client = CreateClient(new StubHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Response(HttpStatusCode.OK, "[]");
        }));

        await client.FetchAllPermissionsAsync();

        Assert.NotNull(body);
        Assert.Contains("response_mode=permissions", body, StringComparison.Ordinal);
        Assert.DoesNotContain("permission=", body, StringComparison.Ordinal);
    }

    // Múltiplos escopos: concede só quando TODOS estão no retrato (modo AllOf).
    [Fact]
    public async Task Snapshot_WhenAllRequestedScopesArePresent_ShouldGrant()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
            "[{\"rsid\":\"1\",\"rsname\":\"document\",\"scopes\":[\"edit\",\"send2sign\"]}]")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.True(Grants(result, "document#edit,send2sign"));
    }

    // Falta um dos escopos pedidos: nega. Conceder o subconjunto seria conceder o que não se pediu.
    [Fact]
    public async Task Snapshot_WhenARequestedScopeIsMissing_ShouldDeny()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
            "[{\"rsid\":\"1\",\"rsname\":\"document\",\"scopes\":[\"edit\"]}]")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.False(Grants(result, "document#edit,send2sign"));
    }

    // Recurso ausente do retrato vira negativa, NUNCA exceção — que sairia do middleware como 500.
    [Fact]
    public async Task Snapshot_WhenTheResourceIsAbsent_ShouldDenyInsteadOfThrowing()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
            "[{\"rsid\":\"1\",\"rsname\":\"other-resource\",\"scopes\":[\"view\"]}]")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.False(Grants(result, "document#edit,send2sign"));
    }

    // Permissão sem escopo ('recurso#') pergunta pelo recurso inteiro: basta ele estar no retrato.
    [Fact]
    public async Task Snapshot_WithoutScopes_ShouldGrantWhenTheResourceIsPresent()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
            "[{\"rsid\":\"1\",\"rsname\":\"document\",\"scopes\":[]}]")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.True(Grants(result, "document#"));
        Assert.False(Grants(result, "document#approve"));
    }

    // O MESMO recurso repetido em entradas diferentes tem os escopos UNIDOS, não sobrescritos:
    // o Keycloak devolve uma entrada por permissão que casou, e sobrescrever perderia escopo
    // concedido — produzindo um 403 que nenhuma configuração do realm explica.
    [Fact]
    public async Task Snapshot_WhenTheSameResourceAppearsTwice_ShouldUnionTheScopes()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
            "[{\"rsid\":\"1\",\"rsname\":\"document\",\"scopes\":[\"view\"]},"
            + "{\"rsid\":\"1\",\"rsname\":\"document\",\"scopes\":[\"approve\"]}]")));

        var result = await client.FetchAllPermissionsAsync();

        Assert.True(Grants(result, "document#view,approve"));
    }

    // A alçada de risco lê o MESMO retrato: é isso que elimina a segunda ida ao Keycloak no approve.
    [Fact]
    public async Task Snapshot_GrantedScopes_ShouldReturnOnlyTheOnesActuallyGranted()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
            "[{\"rsid\":\"1\",\"rsname\":\"document\",\"scopes\":[\"approve\",\"approve-attention\"]}]")));

        var result = await client.FetchAllPermissionsAsync();

        var granted = result.Snapshot.GrantedScopes("document", ["approve-extreme", "approve-danger", "approve-attention"]);

        Assert.Equal(["approve-attention"], granted);
    }
}
