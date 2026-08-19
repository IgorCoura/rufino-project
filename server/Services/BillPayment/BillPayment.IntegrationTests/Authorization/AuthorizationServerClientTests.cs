namespace BillPayment.IntegrationTests.Authorization;

using System.Net;
using System.Text;
using BillPayment.API.Authorization;
using AuthorizationOptions = BillPayment.API.Authorization.AuthorizationOptions;

/// <summary>
/// Tradução da resposta UMA do Keycloak para <see cref="ResourceAccessResult"/>.
/// </summary>
/// <remarks>
/// Sem containers e sem rede: o que se mede é a leitura da resposta do provedor, e um teste que
/// dependesse de um Keycloak no ar mediria se ele está no ar. Mesma doutrina dos adapters do
/// Asaas e do Graph, que já são exercitados por <c>StubHttpMessageHandler</c>.
/// </remarks>
public sealed class AuthorizationServerClientTests
{
    private static AuthorizationServerClient CreateClient(HttpMessageHandler handler)
    {
        var options = new AuthorizationOptions
        {
            AuthServerUrl = "http://keycloak.test",
            Realm = "rufino",
            Resource = "bill-payment-api",
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

    // 401 do Keycloak é o TOKEN recusado, não permissão faltando — e sai como InvalidToken para
    // virar 401 lá na frente, em vez de 403.
    [Fact]
    public async Task VerifyAccess_WhenKeycloakRejectsTheToken_ShouldReturnInvalidToken()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.Unauthorized,
            "{\"error\":\"invalid_grant\",\"error_description\":\"Invalid bearer token\"}")));

        var result = await client.VerifyAccessToResouce("bill#approve");

        Assert.Equal(ResourceAccessResult.InvalidToken, result);
    }

    // 403 access_denied é negativa de permissão legítima.
    [Fact]
    public async Task VerifyAccess_WhenKeycloakAnswersAccessDenied_ShouldReturnDenied()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.Forbidden,
            "{\"error\":\"access_denied\"}")));

        var result = await client.VerifyAccessToResouce("bill#approve");

        Assert.Equal(ResourceAccessResult.Denied, result);
    }

    // 400 também é negativa: pedido malformado não concede nada.
    [Fact]
    public async Task VerifyAccess_WhenKeycloakAnswersBadRequest_ShouldReturnDenied()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.BadRequest,
            "{\"error\":\"invalid_request\"}")));

        var result = await client.VerifyAccessToResouce("bill#approve");

        Assert.Equal(ResourceAccessResult.Denied, result);
    }

    // 5xx é indisponibilidade, e precisa se distinguir de negativa para virar 503.
    [Fact]
    public async Task VerifyAccess_WhenKeycloakAnswersAServerError_ShouldReturnServerUnavailable()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.BadGateway, "")));

        var result = await client.VerifyAccessToResouce("bill#approve");

        Assert.Equal(ResourceAccessResult.ServerUnavailable, result);
    }

    // Falha de rede é indisponibilidade, não exceção subindo pelo middleware como 500.
    [Fact]
    public async Task VerifyAccess_WhenTheRequestFailsAtTheNetworkLevel_ShouldReturnServerUnavailable()
    {
        var client = CreateClient(new ThrowingHandler());

        var result = await client.VerifyAccessToResouce("bill#approve");

        Assert.Equal(ResourceAccessResult.ServerUnavailable, result);
    }

    // Escopo único: o 200 basta, não há RPT a conferir.
    [Fact]
    public async Task VerifyAccess_OnSuccessForASingleScope_ShouldReturnGranted()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK, "{\"result\":true}")));

        var result = await client.VerifyAccessToResouce("bill#approve");

        Assert.Equal(ResourceAccessResult.Granted, result);
    }

    // Múltiplos escopos: concede só quando TODOS estão no RPT (modo AllOf).
    [Fact]
    public async Task VerifyAccess_WhenAllRequestedScopesArePresent_ShouldReturnGranted()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
            "[{\"rsid\":\"1\",\"rsname\":\"capture-source\",\"scopes\":[\"manage\",\"sync\"]}]")));

        var result = await client.VerifyAccessToResouce("capture-source#manage,sync");

        Assert.Equal(ResourceAccessResult.Granted, result);
    }

    // Falta um dos escopos pedidos: nega. Conceder o subconjunto seria conceder o que não se pediu.
    [Fact]
    public async Task VerifyAccess_WhenARequestedScopeIsMissing_ShouldReturnDenied()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
            "[{\"rsid\":\"1\",\"rsname\":\"capture-source\",\"scopes\":[\"manage\"]}]")));

        var result = await client.VerifyAccessToResouce("capture-source#manage,sync");

        Assert.Equal(ResourceAccessResult.Denied, result);
    }

    // Recurso ausente do RPT vira negativa, NUNCA exceção — que sairia do middleware como 500.
    [Fact]
    public async Task VerifyAccess_WhenTheResourceIsAbsentFromTheRpt_ShouldReturnDeniedInsteadOfThrowing()
    {
        var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
            "[{\"rsid\":\"1\",\"rsname\":\"other-resource\",\"scopes\":[\"view\"]}]")));

        var result = await client.VerifyAccessToResouce("capture-source#manage,sync");

        Assert.Equal(ResourceAccessResult.Denied, result);
    }
}
