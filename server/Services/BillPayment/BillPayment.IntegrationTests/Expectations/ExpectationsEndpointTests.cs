namespace BillPayment.IntegrationTests.Expectations;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// A expectativa pela porta da frente. Os oito endpoints do controller não tinham um único teste
/// HTTP até 2026-08-28 — a suíte de expectativa chamava os commands direto.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class ExpectationsEndpointTests : BaseIntegrationTest
{
    private static readonly Guid Tenant = TestTenants.Primary;
    private static readonly Guid OtherTenant = TestTenants.Secondary;
    private static readonly Guid Decider = new("0195a1f0-0000-7000-8000-0000000000a7");
    private static readonly DateTime OccurredAt = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    private const string PayeeCnpj = "11444777000161";

    public ExpectationsEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private static Uri RouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/expectations", UriKind.Relative);

    // Cadastrar pela API grava a expectativa com a antecedência derivada e a devolve na lista e
    // no detalhe.
    [Fact]
    public async Task PostAndGet_ShouldRoundTripThroughTheApi()
    {
        var payeeId = await SeedPayeeAsync(Tenant);

        var created = await PostAsync(Tenant, Request(payeeId, "L18502"));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotEqual(Guid.Empty, body!.Id);

        var page = await Client.GetFromJsonAsync<Page>(RouteFor(Tenant));
        var listed = Assert.Single(page!.Items);
        Assert.Equal("L18502", listed.AccountReference);

        var detail = await Client.GetFromJsonAsync<Dto>(new Uri($"{RouteFor(Tenant)}/{body.Id}", UriKind.Relative));
        Assert.Equal("Conta L18502", detail!.Label);
        Assert.Equal("Monthly", detail.Recurrence);
        Assert.True(detail.IsActive);
    }

    // Beneficiário que não existe no tenant é referência inválida — 404, como qualquer agregado.
    [Fact]
    public async Task Post_WithAnUnknownPayee_ShouldReturnNotFound()
    {
        var response = await PostAsync(Tenant, Request(new Guid("0195a1f0-0000-7000-8000-0000000000e9"), "X"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Editar pela API troca rótulo e dia, e o detalhe reflete.
    [Fact]
    public async Task Put_ShouldEditAndPersist()
    {
        var payeeId = await SeedPayeeAsync(Tenant);
        var id = await RegisterAsync(Tenant, payeeId, "L18502");

        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(Tenant)}/{id}", UriKind.Relative),
            new EditRequest("L18502", "DAE - Casa", "Monthly", 15, 10, null, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await Client.GetFromJsonAsync<Dto>(new Uri($"{RouteFor(Tenant)}/{id}", UriKind.Relative));
        Assert.Equal("DAE - Casa", detail!.Label);
        Assert.Equal(15, detail.ExpectedDueDay);
    }

    // Pausar a vigilância pela API desativa sem apagar.
    [Fact]
    public async Task PutWatch_ShouldPauseWithoutDeleting()
    {
        var payeeId = await SeedPayeeAsync(Tenant);
        var id = await RegisterAsync(Tenant, payeeId, "L18502");

        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(Tenant)}/{id}/watch", UriKind.Relative),
            new WatchRequest(false, null, "imóvel vendido"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await Client.GetFromJsonAsync<Dto>(new Uri($"{RouteFor(Tenant)}/{id}", UriKind.Relative));
        Assert.False(detail!.IsActive);
    }

    // O painel de pendências responde vazio para quem não tem ciclo em aberto.
    [Fact]
    public async Task GetPending_ShouldAnswerEvenWhenNothingIsPending()
    {
        var response = await Client.GetAsync(new Uri($"{RouteFor(Tenant)}/pending", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Dispensar um ciclo que não existe é recusado pelo domínio (BLP.EXP10), não com 500.
    [Fact]
    public async Task PostWaive_WithAnUnknownCycle_ShouldBeRefusedByTheDomain()
    {
        var payeeId = await SeedPayeeAsync(Tenant);
        var id = await RegisterAsync(Tenant, payeeId, "L18502");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"{RouteFor(Tenant)}/{id}/cycles/{new Guid("0195a1f0-0000-7000-8000-0000000000c9")}/waive", UriKind.Relative))
        {
            Content = JsonContent.Create(new WaiveRequest("já paguei por fora")),
        };
        request.Headers.Add("x-user-id", Decider.ToString());
        request.Headers.Add("x-requestid", Guid.CreateVersion7().ToString());

        var response = await Client.SendAsync(request);

        Assert.True((int)response.StatusCode is >= 400 and < 500, $"Esperava 4xx, veio {(int)response.StatusCode}.");
        Assert.Contains("BLP.EXP10", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    // Excluir pela API remove a expectativa; o detalhe passa a 404.
    [Fact]
    public async Task Delete_ShouldRemoveTheExpectation()
    {
        var payeeId = await SeedPayeeAsync(Tenant);
        var id = await RegisterAsync(Tenant, payeeId, "L18502");

        var response = await Client.DeleteAsync(new Uri($"{RouteFor(Tenant)}/{id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await Client.GetAsync(new Uri($"{RouteFor(Tenant)}/{id}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal(0, await ExecuteDbContextAsync(db => db.BillExpectations.CountAsync()));
    }

    // Isolamento pela borda: a expectativa de um tenant não aparece na lista nem no detalhe do
    // outro, e editar pela rota do outro é 404.
    [Fact]
    public async Task AnotherTenant_ShouldNotSeeNorEditTheExpectation()
    {
        var payeeId = await SeedPayeeAsync(Tenant);
        var id = await RegisterAsync(Tenant, payeeId, "L18502");

        var page = await Client.GetFromJsonAsync<Page>(RouteFor(OtherTenant));
        Assert.Empty(page!.Items);

        var detail = await Client.GetAsync(new Uri($"{RouteFor(OtherTenant)}/{id}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);

        var edit = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(OtherTenant)}/{id}", UriKind.Relative),
            new EditRequest("L18502", "INVASOR", "Monthly", 15, 10, null, null));
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
    }

    private static RegisterRequest Request(Guid payeeId, string reference)
        => new(payeeId, reference, $"Conta {reference}", "Monthly", 10, 8, null, null, null);

    private Task<HttpResponseMessage> PostAsync(Guid tenantId, RegisterRequest request)
        => Client.PostAsJsonAsync(RouteFor(tenantId), request);

    private async Task<Guid> RegisterAsync(Guid tenantId, Guid payeeId, string reference)
    {
        var response = await PostAsync(tenantId, Request(payeeId, reference));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!.Id;
    }

    private Task<Guid> SeedPayeeAsync(Guid tenantId)
        => ExecuteDbContextAsync(async db =>
        {
            var payee = Payee.Register(
                TenantId.From(tenantId), "CONCESSIONARIA EXEMPLO SA", TaxId.Parse(PayeeCnpj), AmountPolicy.Unbounded(), OccurredAt);

            await db.Payees.AddAsync(payee);
            await db.SaveEntitiesAsync();
            return payee.Id.Value;
        });

    // DTOs duplicados de propósito: o teste protege o contrato, não o reusa.
    private sealed record RegisterRequest(
        Guid PayeeId, string? AccountReference, string Label, string Recurrence, int ExpectedDueDay,
        int ObservedLeadDays, int? AlertLeadDays, DateOnly? FirstDueDate, Guid? HintSourceId);

    private sealed record EditRequest(
        string? AccountReference, string Label, string Recurrence, int ExpectedDueDay,
        int ObservedLeadDays, int? AlertLeadDays, DateOnly? FirstDueDate);

    private sealed record WatchRequest(bool IsActive, DateOnly? PausedUntil, string? Reason);

    private sealed record WaiveRequest(string? Reason);

    private sealed record RegisterResponse(Guid Id, string Label, int AlertLeadDays);

    private sealed record Dto(
        Guid Id, Guid PayeeId, string AccountReference, string Label, string Recurrence, int ExpectedDueDay,
        int ObservedLeadDays, int AlertLeadDays, string Origin, int ObservationCount, bool IsActive, DateOnly? PausedUntil);

    private sealed record Page(IReadOnlyCollection<Dto> Items, string? NextCursor);
}
