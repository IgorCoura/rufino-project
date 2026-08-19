namespace BillPayment.IntegrationTests.TrustedOrigins;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RegisterTrustedOriginTests : BaseIntegrationTest
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid DecidedBy = new("0195a1f0-0000-7000-8000-0000000000a1");

    private static Uri Route => new($"/api/v1/{TenantId}/trusted-origins", UriKind.Relative);

    // Quem decide vem do sub do token — o dublê de autenticação o traduz do header x-user-id.
    public RegisterTrustedOriginTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        Client.DefaultRequestHeaders.Add(MockAuthenticationHandler.UserIdHeader, DecidedBy.ToString());
    }

    // Cadastrar uma origem confiável responde 200 e grava a linha com o valor normalizado.
    [Fact]
    public async Task PostTrustedOrigin_WithValidPayload_ShouldPersistNormalizedValue()
    {
        var request = new RegisterTrustedOriginRequest(
            "EmailAddress", "  FINANCEIRO@Fornecedor.COM.BR ", "Trusted", "cadastrado no onboarding");

        var response = await Client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TrustedOriginIdResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);

        // Lê em scope novo com AsNoTracking: reusar o contexto da requisição devolveria
        // a entidade do change tracker e o teste passaria mesmo sem ter persistido.
        var persisted = await ExecuteDbContextAsync(db => db.TrustedOrigins
            .AsNoTracking()
            .SingleAsync(o => o.Id == TrustedOriginId.From(body.Id)));

        Assert.Equal("financeiro@fornecedor.com.br", persisted.Value);
        Assert.Same(OriginKind.EmailAddress, persisted.Kind);
        Assert.Same(TrustDecision.Trusted, persisted.Decision);
        Assert.Equal("cadastrado no onboarding", persisted.Note);
        Assert.Equal(DecidedBy, persisted.DecidedBy.Value);
        Assert.Equal(TenantId, persisted.TenantId.Value);
    }

    // Regressão da remoção do decidedBy do contrato (2026-08-17): um corpo que ainda mande o
    // campo não forja a autoria — quem vale é o sub do token, e o campo extra é ignorado.
    [Fact]
    public async Task PostTrustedOrigin_WithDecidedByInTheBody_ShouldIgnoreItAndUseTheToken()
    {
        var forged = new Guid("0195a1f0-0000-7000-8000-0000000000c9");
        var payload = new
        {
            kind = "EmailAddress",
            value = "financeiro@fornecedor.com.br",
            decision = "Trusted",
            decidedBy = forged,
            note = (string?)null,
        };

        var response = await Client.PostAsJsonAsync(Route, payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TrustedOriginIdResponse>();

        var persisted = await ExecuteDbContextAsync(db => db.TrustedOrigins
            .AsNoTracking()
            .SingleAsync(o => o.Id == TrustedOriginId.From(body!.Id)));

        Assert.Equal(DecidedBy, persisted.DecidedBy.Value);
        Assert.NotEqual(forged, persisted.DecidedBy.Value);
    }

    // Token sem sub utilizável não decide: o domínio recusa com BLP.ORG10 e nada é gravado.
    [Fact]
    public async Task PostTrustedOrigin_WithoutAUsableSub_ShouldReturnBadRequest()
    {
        using var client = Factory.CreateClient().Authenticated();

        var response = await client.PostAsJsonAsync(Route, new RegisterTrustedOriginRequest(
            "EmailAddress", "financeiro@fornecedor.com.br", "Trusted", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.ORG10", error!.Id);

        Assert.Equal(0, await ExecuteDbContextAsync(db => db.TrustedOrigins.CountAsync()));
    }

    // Cadastrar a mesma origem duas vezes responde 409 e não cria linha duplicada — BLP.ORG01.
    [Fact]
    public async Task PostTrustedOrigin_WhenAlreadyRegistered_ShouldReturnConflict()
    {
        var request = new RegisterTrustedOriginRequest(
            "EmailDomain", "fornecedor.com.br", "Trusted", null);

        var first = await Client.PostAsJsonAsync(Route, request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await Client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var error = await second.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.ORG01", error!.Id);

        var count = await ExecuteDbContextAsync(db => db.TrustedOrigins.CountAsync());
        Assert.Equal(1, count);
    }

    // A duplicata é detectada mesmo quando o valor chega com caixa e espaços diferentes.
    [Fact]
    public async Task PostTrustedOrigin_WhenDuplicateDiffersOnlyByCasing_ShouldReturnConflict()
    {
        await Client.PostAsJsonAsync(Route, new RegisterTrustedOriginRequest(
            "EmailDomain", "fornecedor.com.br", "Trusted", null));

        var second = await Client.PostAsJsonAsync(Route, new RegisterTrustedOriginRequest(
            "EmailDomain", "  FORNECEDOR.COM.BR  ", "Blocked", null));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // Valor que não é endereço nem domínio válido responde 400 e não grava nada.
    [Theory]
    [InlineData("EmailAddress", "sem-arroba.com.br", "BLP.ORG07")]
    [InlineData("EmailDomain", "fornecedor", "BLP.ORG08")]
    [InlineData("EmailAddress", "   ", "BLP.ORG05")]
    public async Task PostTrustedOrigin_WithInvalidValue_ShouldReturnBadRequest(
        string kind, string value, string expectedErrorId)
    {
        var response = await Client.PostAsJsonAsync(Route, new RegisterTrustedOriginRequest(
            kind, value, "Trusted", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal(expectedErrorId, error!.Id);

        Assert.Equal(0, await ExecuteDbContextAsync(db => db.TrustedOrigins.CountAsync()));
    }

    // Tipo ou decisão fora do catálogo responde 400 — a tradução do Smart Enum falha no handler.
    [Theory]
    [InlineData("NaoExiste", "Trusted")]
    [InlineData("EmailAddress", "Talvez")]
    public async Task PostTrustedOrigin_WithUnknownEnumeration_ShouldReturnBadRequest(string kind, string decision)
    {
        var response = await Client.PostAsJsonAsync(Route, new RegisterTrustedOriginRequest(
            kind, "financeiro@fornecedor.com.br", decision, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // O mesmo x-requestid não cria duas origens: a segunda chamada volta com Id vazio.
    [Fact]
    public async Task PostTrustedOrigin_WithRepeatedRequestId_ShouldBeIdempotent()
    {
        var requestId = new Guid("0195a1f0-0000-7000-8000-0000000000f1");
        var payload = new RegisterTrustedOriginRequest(
            "EmailAddress", "financeiro@fornecedor.com.br", "Trusted", null);

        using var first = BuildRequest(payload, requestId);
        var firstResponse = await Client.SendAsync(first);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using var second = BuildRequest(payload, requestId);
        var secondResponse = await Client.SendAsync(second);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadFromJsonAsync<TrustedOriginIdResponse>();
        Assert.Equal(Guid.Empty, body!.Id);

        Assert.Equal(1, await ExecuteDbContextAsync(db => db.TrustedOrigins.CountAsync()));
    }

    private static HttpRequestMessage BuildRequest(RegisterTrustedOriginRequest payload, Guid requestId)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, Route)
        {
            Content = JsonContent.Create(payload),
        };
        message.Headers.Add("x-requestid", requestId.ToString());
        return message;
    }
}
