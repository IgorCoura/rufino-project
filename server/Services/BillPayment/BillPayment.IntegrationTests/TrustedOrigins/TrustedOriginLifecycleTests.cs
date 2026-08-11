namespace BillPayment.IntegrationTests.TrustedOrigins;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class TrustedOriginLifecycleTests : BaseIntegrationTest
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid OtherTenantId = new("0195a1f0-0000-7000-8000-000000000002");
    private static readonly Guid DecidedBy = new("0195a1f0-0000-7000-8000-0000000000a1");
    private static readonly Guid AnotherUser = new("0195a1f0-0000-7000-8000-0000000000b2");
    private static readonly Guid UnknownId = new("0195a1f0-0000-7000-8000-0000000000ff");

    private static Uri RouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/trusted-origins", UriKind.Relative);

    public TrustedOriginLifecycleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Promover uma origem bloqueada a confiável grava a nova decisão, o autor e a observação.
    [Fact]
    public async Task PutDecision_WhenOriginExists_ShouldReplaceDecisionAndAudit()
    {
        var id = await RegisterAsync("EmailAddress", "financeiro@fornecedor.com.br", "Blocked");

        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{id}/decision", UriKind.Relative),
            new ChangeTrustedOriginDecisionRequest("Trusted", AnotherUser, "revisado por telefone"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var persisted = await ExecuteDbContextAsync(db => db.TrustedOrigins
            .AsNoTracking()
            .SingleAsync(o => o.Id == TrustedOriginId.From(id)));

        Assert.Same(TrustDecision.Trusted, persisted.Decision);
        Assert.Equal(AnotherUser, persisted.DecidedBy.Value);
        Assert.Equal("revisado por telefone", persisted.Note);
    }

    // Alterar a decisão de uma origem inexistente responde 404 — BLP.ORG02.
    [Fact]
    public async Task PutDecision_WhenOriginDoesNotExist_ShouldReturnNotFound()
    {
        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{UnknownId}/decision", UriKind.Relative),
            new ChangeTrustedOriginDecisionRequest("Trusted", DecidedBy, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.ORG02", error!.Id);
    }

    // Origem de um tenant é invisível para outro: alterá-la de fora responde 404, não 200.
    [Fact]
    public async Task PutDecision_WhenOriginBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        var id = await RegisterAsync("EmailAddress", "financeiro@fornecedor.com.br", "Trusted");

        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(OtherTenantId)}/{id}/decision", UriKind.Relative),
            new ChangeTrustedOriginDecisionRequest("Blocked", DecidedBy, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Remover uma origem apaga a linha.
    [Fact]
    public async Task DeleteTrustedOrigin_WhenOriginExists_ShouldRemoveRow()
    {
        var id = await RegisterAsync("EmailDomain", "fornecedor.com.br", "Trusted");

        var response = await Client.DeleteAsync(new Uri($"{RouteFor(TenantId)}/{id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await ExecuteDbContextAsync(db => db.TrustedOrigins.CountAsync()));
    }

    // Remover origem de outro tenant responde 404 e não apaga nada.
    [Fact]
    public async Task DeleteTrustedOrigin_WhenOriginBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        var id = await RegisterAsync("EmailDomain", "fornecedor.com.br", "Trusted");

        var response = await Client.DeleteAsync(new Uri($"{RouteFor(OtherTenantId)}/{id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, await ExecuteDbContextAsync(db => db.TrustedOrigins.CountAsync()));
    }

    // Buscar por id devolve a origem cadastrada.
    [Fact]
    public async Task GetById_WhenOriginExists_ShouldReturnIt()
    {
        var id = await RegisterAsync("EmailAddress", "financeiro@fornecedor.com.br", "Trusted");

        var origin = await Client.GetFromJsonAsync<TrustedOriginResponse>(
            new Uri($"{RouteFor(TenantId)}/{id}", UriKind.Relative));

        Assert.Equal(id, origin!.Id);
        Assert.Equal("financeiro@fornecedor.com.br", origin.Value);
        Assert.Equal("EmailAddress", origin.Kind);
        Assert.Equal("Trusted", origin.Decision);
    }

    // Buscar por id de outro tenant responde 404 — nenhum vazamento entre contas.
    [Fact]
    public async Task GetById_WhenOriginBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        var id = await RegisterAsync("EmailAddress", "financeiro@fornecedor.com.br", "Trusted");

        var response = await Client.GetAsync(new Uri($"{RouteFor(OtherTenantId)}/{id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // A listagem só devolve as origens do tenant da rota.
    [Fact]
    public async Task GetList_ShouldReturnOnlyOriginsOfTheRouteTenant()
    {
        await RegisterAsync("EmailAddress", "financeiro@fornecedor.com.br", "Trusted");
        await RegisterAsync("EmailDomain", "fornecedor.com.br", "Trusted");
        await RegisterAsync("EmailDomain", "outro.com.br", "Trusted", OtherTenantId);

        var page = await Client.GetFromJsonAsync<TrustedOriginPageResponse>(RouteFor(TenantId));

        Assert.Equal(2, page!.Items.Count);
        Assert.All(page.Items, o => Assert.DoesNotContain("outro.com.br", o.Value, StringComparison.Ordinal));
    }

    // A paginação por cursor devolve o restante sem repetir o que já veio.
    [Fact]
    public async Task GetList_WithCursor_ShouldPaginateWithoutRepeating()
    {
        await RegisterAsync("EmailDomain", "a-fornecedor.com.br", "Trusted");
        await RegisterAsync("EmailDomain", "b-fornecedor.com.br", "Trusted");
        await RegisterAsync("EmailDomain", "c-fornecedor.com.br", "Trusted");

        var firstPage = await Client.GetFromJsonAsync<TrustedOriginPageResponse>(
            new Uri($"{RouteFor(TenantId)}?limit=2", UriKind.Relative));

        Assert.Equal(2, firstPage!.Items.Count);
        Assert.NotNull(firstPage.NextCursor);

        var secondPage = await Client.GetFromJsonAsync<TrustedOriginPageResponse>(
            new Uri($"{RouteFor(TenantId)}?limit=2&cursor={Uri.EscapeDataString(firstPage.NextCursor!)}", UriKind.Relative));

        Assert.Single(secondPage!.Items);
        Assert.DoesNotContain(secondPage.Items[0].Id, firstPage.Items.Select(i => i.Id));
    }

    // Resolver um remetente casa pelo domínio cadastrado.
    [Fact]
    public async Task GetResolve_WhenDomainIsRegistered_ShouldMatchAnyAddressOfThatDomain()
    {
        await RegisterAsync("EmailDomain", "fornecedor.com.br", "Trusted");

        var origin = await Client.GetFromJsonAsync<TrustedOriginResponse>(
            new Uri($"{RouteFor(TenantId)}/resolve?sender=cobranca@fornecedor.com.br", UriKind.Relative));

        Assert.Equal("fornecedor.com.br", origin!.Value);
        Assert.Equal("EmailDomain", origin.Kind);
    }

    // Havendo endereço e domínio cadastrados, o endereço exato vence.
    [Fact]
    public async Task GetResolve_WhenBothAddressAndDomainMatch_ShouldPreferTheExactAddress()
    {
        await RegisterAsync("EmailDomain", "fornecedor.com.br", "Trusted");
        await RegisterAsync("EmailAddress", "financeiro@fornecedor.com.br", "Blocked");

        var origin = await Client.GetFromJsonAsync<TrustedOriginResponse>(
            new Uri($"{RouteFor(TenantId)}/resolve?sender=financeiro@fornecedor.com.br", UriKind.Relative));

        Assert.Equal("EmailAddress", origin!.Kind);
        Assert.Equal("Blocked", origin.Decision);
    }

    // Origem desconhecida responde 204 — é estado válido e comum, não erro.
    [Fact]
    public async Task GetResolve_WhenSenderIsUnknown_ShouldReturnNoContent()
    {
        await RegisterAsync("EmailDomain", "fornecedor.com.br", "Trusted");

        var response = await Client.GetAsync(
            new Uri($"{RouteFor(TenantId)}/resolve?sender=alguem@desconhecido.com.br", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<Guid> RegisterAsync(string kind, string value, string decision, Guid? tenantId = null)
    {
        var response = await Client.PostAsJsonAsync(
            RouteFor(tenantId ?? TenantId),
            new RegisterTrustedOriginRequest(kind, value, decision, DecidedBy, null));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TrustedOriginIdResponse>();
        return body!.Id;
    }
}
