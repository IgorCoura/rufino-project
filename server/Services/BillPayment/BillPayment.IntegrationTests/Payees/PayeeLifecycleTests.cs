namespace BillPayment.IntegrationTests.Payees;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Payees;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class PayeeLifecycleTests : BaseIntegrationTest
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid OtherTenantId = new("0195a1f0-0000-7000-8000-000000000002");
    private static readonly Guid UnknownId = new("0195a1f0-0000-7000-8000-0000000000ff");

    private const string Cnpj = "11222333000181";
    private const string CnpjFormatted = "11.222.333/0001-81";
    private const string OtherCnpj = "11444777000161";

    private static Uri RouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/payees", UriKind.Relative);

    public PayeeLifecycleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Cadastrar um beneficiário grava documento sanitizado, política de valor e estado ativo.
    [Fact]
    public async Task PostPayee_WithRangePolicy_ShouldPersistSanitizedTaxIdAndPolicy()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "ENERGIA DO VALE S.A.", CnpjFormatted, "Range", null, null, 80m, 400m));

        var persisted = await ExecuteDbContextAsync(db => db.Payees
            .AsNoTracking()
            .SingleAsync(p => p.Id == PayeeId.From(id)));

        Assert.Equal(Cnpj, persisted.TaxId.Value);
        Assert.Same(AmountPolicyKind.Range, persisted.AmountPolicy.Kind);
        Assert.Equal(80m, persisted.AmountPolicy.MinAmount!.Amount);
        Assert.Equal(400m, persisted.AmountPolicy.MaxAmount!.Amount);
        Assert.True(persisted.IsActive);
    }

    // A política de valor fixo sobrevive à ida e volta do banco com valor e tolerância intactos.
    [Fact]
    public async Task PostPayee_WithFixedPolicy_ShouldRoundTripAmountAndTolerance()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "IMOBILIARIA CENTRAL", Cnpj, "Fixed", 1500.55m, 5m, null, null));

        var payee = await Client.GetFromJsonAsync<PayeeResponse>(
            new Uri($"{RouteFor(TenantId)}/{id}", UriKind.Relative));

        Assert.Equal("Fixed", payee!.AmountPolicy.Kind);
        Assert.Equal(1500.55m, payee.AmountPolicy.ExpectedAmount);
        Assert.Equal(5m, payee.AmountPolicy.TolerancePercent);
        Assert.True(payee.AmountPolicy.IsConclusive);
    }

    // Política sem expectativa é inconclusiva — o check de valor não pode aprovar por ela.
    [Fact]
    public async Task PostPayee_WithUnboundedPolicy_ShouldBeInconclusive()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "CONCESSIONARIA X", Cnpj, "Unbounded", null, null, null, null));

        var payee = await Client.GetFromJsonAsync<PayeeResponse>(
            new Uri($"{RouteFor(TenantId)}/{id}", UriKind.Relative));

        Assert.Equal("Unbounded", payee!.AmountPolicy.Kind);
        Assert.False(payee.AmountPolicy.IsConclusive);
    }

    // Faltando os valores que o tipo exige, o cadastro é recusado — BLP.PYE07.
    [Fact]
    public async Task PostPayee_WithFixedPolicyMissingAmount_ShouldReturnBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            RouteFor(TenantId),
            new RegisterPayeeRequest("IMOBILIARIA CENTRAL", Cnpj, "Fixed", null, 5m, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PYE07", error!.Id);
    }

    // Documento com dígito verificador inválido é recusado antes de qualquer gravação.
    [Fact]
    public async Task PostPayee_WithInvalidCheckDigit_ShouldReturnBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            RouteFor(TenantId),
            new RegisterPayeeRequest("FORNECEDOR", "11222333000180", "Unbounded", null, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ExecuteDbContextAsync(db => db.Payees.CountAsync()));
    }

    // O mesmo documento duas vezes no mesmo tenant é conflito — BLP.PYE01.
    [Fact]
    public async Task PostPayee_WithDuplicateTaxId_ShouldReturnConflict()
    {
        await RegisterAsync(new RegisterPayeeRequest("SECONCI", Cnpj, "Unbounded", null, null, null, null));

        var response = await Client.PostAsJsonAsync(
            RouteFor(TenantId),
            new RegisterPayeeRequest("SECONCI SP", CnpjFormatted, "Unbounded", null, null, null, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PYE01", error!.Id);
    }

    // O mesmo documento em tenants diferentes é permitido — a unicidade é por tenant.
    [Fact]
    public async Task PostPayee_WithSameTaxIdInAnotherTenant_ShouldBeAccepted()
    {
        await RegisterAsync(new RegisterPayeeRequest("SECONCI", Cnpj, "Unbounded", null, null, null, null));

        var response = await Client.PostAsJsonAsync(
            RouteFor(OtherTenantId),
            new RegisterPayeeRequest("SECONCI", Cnpj, "Unbounded", null, null, null, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, await ExecuteDbContextAsync(db => db.Payees.CountAsync()));
    }

    // Apelidos e bancos aceitos sobrevivem à ida e volta do banco na ordem em que entraram.
    [Fact]
    public async Task AliasesAndBanks_ShouldRoundTripThroughThePersistedCollections()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "SECONCI SAO PAULO", Cnpj, "Unbounded", null, null, null, null));

        await PostAsync($"{id}/aliases", new PayeeAliasRequest("SERVICO SOCIAL DA CONSTRUCAO"));
        await PostAsync($"{id}/aliases", new PayeeAliasRequest("SECONCI SP"));
        await PostAsync($"{id}/accepted-banks", new PayeeBankRequest("341"));
        await PostAsync($"{id}/accepted-banks", new PayeeBankRequest("33"));

        var payee = await Client.GetFromJsonAsync<PayeeResponse>(
            new Uri($"{RouteFor(TenantId)}/{id}", UriKind.Relative));

        Assert.Equal(["SERVICO SOCIAL DA CONSTRUCAO", "SECONCI SP"], payee!.Aliases);

        // "33" foi normalizado para "033" pelo Value Object antes de ser gravado.
        Assert.Equal(["341", "033"], payee.AcceptedBanks);
    }

    // Remover apelido e banco esvazia as coleções sem apagar o beneficiário.
    [Fact]
    public async Task DeleteAliasAndBank_ShouldEmptyTheCollections()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "SECONCI SAO PAULO", Cnpj, "Unbounded", null, null, null, null));
        await PostAsync($"{id}/aliases", new PayeeAliasRequest("SECONCI SP"));
        await PostAsync($"{id}/accepted-banks", new PayeeBankRequest("341"));

        await Client.DeleteAsync(new Uri($"{RouteFor(TenantId)}/{id}/aliases?alias={Uri.EscapeDataString("SECONCI SP")}", UriKind.Relative));
        await Client.DeleteAsync(new Uri($"{RouteFor(TenantId)}/{id}/accepted-banks/341", UriKind.Relative));

        var payee = await Client.GetFromJsonAsync<PayeeResponse>(
            new Uri($"{RouteFor(TenantId)}/{id}", UriKind.Relative));

        Assert.Empty(payee!.Aliases);
        Assert.Empty(payee.AcceptedBanks);
    }

    // Trocar a política de um beneficiário já persistido substitui a coluna inteira.
    // Regressão: com owned type aninhado o Money viria NULL nesse cenário — por isso a
    // política é gravada como uma coluna jsonb só.
    [Fact]
    public async Task PutAmountPolicy_OnPersistedPayee_ShouldReplaceTheWholePolicy()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "IMOBILIARIA CENTRAL", Cnpj, "Range", null, null, 80m, 400m));

        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{id}/amount-policy", UriKind.Relative),
            new AlterPayeeAmountPolicyRequest("Fixed", 1200m, 3m, null, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var persisted = await ExecuteDbContextAsync(db => db.Payees
            .AsNoTracking()
            .SingleAsync(p => p.Id == PayeeId.From(id)));

        Assert.Same(AmountPolicyKind.Fixed, persisted.AmountPolicy.Kind);
        Assert.NotNull(persisted.AmountPolicy.ExpectedAmount);
        Assert.Equal(1200m, persisted.AmountPolicy.ExpectedAmount!.Amount);
        Assert.Null(persisted.AmountPolicy.MinAmount);
    }

    // Desativar bloqueia alteração; reativar libera de novo — BLP.PYE16 no meio.
    [Fact]
    public async Task PutActivation_ShouldGateChangesWhileInactive()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "SECONCI", Cnpj, "Unbounded", null, null, null, null));

        await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{id}/activation", UriKind.Relative),
            new AlterPayeeActivationRequest(false));

        var blocked = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{id}/legal-name", UriKind.Relative),
            new RenamePayeeRequest("NOVO NOME"));

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var error = await blocked.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PYE16", error!.Id);

        await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{id}/activation", UriKind.Relative),
            new AlterPayeeActivationRequest(true));

        var allowed = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{id}/legal-name", UriKind.Relative),
            new RenamePayeeRequest("NOVO NOME"));

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    // Buscar pelo documento encontra o beneficiário mesmo com o texto formatado.
    [Fact]
    public async Task GetByTaxId_WithFormattedDocument_ShouldFindThePayee()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "SECONCI", Cnpj, "Unbounded", null, null, null, null));

        var payee = await Client.GetFromJsonAsync<PayeeResponse>(
            new Uri($"{RouteFor(TenantId)}/by-tax-id?taxId={Uri.EscapeDataString(CnpjFormatted)}", UriKind.Relative));

        Assert.Equal(id, payee!.Id);
    }

    // Documento sem cadastro responde 204 — ausência é o que torna o check inconclusivo, não erro.
    [Fact]
    public async Task GetByTaxId_WhenPayeeIsUnknown_ShouldReturnNoContent()
    {
        await RegisterAsync(new RegisterPayeeRequest("SECONCI", Cnpj, "Unbounded", null, null, null, null));

        var response = await Client.GetAsync(
            new Uri($"{RouteFor(TenantId)}/by-tax-id?taxId={OtherCnpj}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // Documento malformado na busca é ausência, não erro 500.
    [Fact]
    public async Task GetByTaxId_WithMalformedDocument_ShouldReturnNoContent()
    {
        var response = await Client.GetAsync(
            new Uri($"{RouteFor(TenantId)}/by-tax-id?taxId=123", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // Beneficiário de um tenant é invisível para outro: buscá-lo de fora responde 404.
    [Fact]
    public async Task GetById_WhenPayeeBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "SECONCI", Cnpj, "Unbounded", null, null, null, null));

        var response = await Client.GetAsync(new Uri($"{RouteFor(OtherTenantId)}/{id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Alterar beneficiário inexistente responde 404 — BLP.PYE02.
    [Fact]
    public async Task PutLegalName_WhenPayeeDoesNotExist_ShouldReturnNotFound()
    {
        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{UnknownId}/legal-name", UriKind.Relative),
            new RenamePayeeRequest("QUALQUER"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PYE02", error!.Id);
    }

    // A listagem só devolve os beneficiários do tenant da rota.
    [Fact]
    public async Task GetList_ShouldReturnOnlyPayeesOfTheRouteTenant()
    {
        await RegisterAsync(new RegisterPayeeRequest("SECONCI", Cnpj, "Unbounded", null, null, null, null));
        await RegisterAsync(new RegisterPayeeRequest("OUTRO", OtherCnpj, "Unbounded", null, null, null, null));
        await RegisterAsync(
            new RegisterPayeeRequest("DE OUTRO TENANT", Cnpj, "Unbounded", null, null, null, null),
            OtherTenantId);

        var page = await Client.GetFromJsonAsync<PayeePageResponse>(RouteFor(TenantId));

        Assert.Equal(2, page!.Items.Count);
        Assert.All(page.Items, p => Assert.DoesNotContain("DE OUTRO TENANT", p.LegalName, StringComparison.Ordinal));
    }

    // A paginação por cursor devolve o restante sem repetir o que já veio.
    [Fact]
    public async Task GetList_WithCursor_ShouldPaginateWithoutRepeating()
    {
        await RegisterAsync(new RegisterPayeeRequest("A", Cnpj, "Unbounded", null, null, null, null));
        await RegisterAsync(new RegisterPayeeRequest("B", OtherCnpj, "Unbounded", null, null, null, null));
        await RegisterAsync(new RegisterPayeeRequest("C", "11222333000262", "Unbounded", null, null, null, null));

        var firstPage = await Client.GetFromJsonAsync<PayeePageResponse>(
            new Uri($"{RouteFor(TenantId)}?limit=2", UriKind.Relative));

        Assert.Equal(2, firstPage!.Items.Count);
        Assert.NotNull(firstPage.NextCursor);

        var secondPage = await Client.GetFromJsonAsync<PayeePageResponse>(
            new Uri($"{RouteFor(TenantId)}?limit=2&cursor={Uri.EscapeDataString(firstPage.NextCursor!)}", UriKind.Relative));

        Assert.Single(secondPage!.Items);
        Assert.DoesNotContain(secondPage.Items[0].Id, firstPage.Items.Select(i => i.Id));
    }

    // Remover apaga a linha; remover de outro tenant não apaga nada.
    [Fact]
    public async Task DeletePayee_ShouldRemoveOnlyWithinTheOwningTenant()
    {
        var id = await RegisterAsync(new RegisterPayeeRequest(
            "SECONCI", Cnpj, "Unbounded", null, null, null, null));

        var fromOther = await Client.DeleteAsync(new Uri($"{RouteFor(OtherTenantId)}/{id}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, fromOther.StatusCode);
        Assert.Equal(1, await ExecuteDbContextAsync(db => db.Payees.CountAsync()));

        var fromOwner = await Client.DeleteAsync(new Uri($"{RouteFor(TenantId)}/{id}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, fromOwner.StatusCode);
        Assert.Equal(0, await ExecuteDbContextAsync(db => db.Payees.CountAsync()));
    }

    // Repetir o mesmo x-requestid não cadastra duas vezes — idempotência do IdentifiedCommand.
    [Fact]
    public async Task PostPayee_WithSameRequestId_ShouldRegisterOnlyOnce()
    {
        var requestId = new Guid("0195a1f0-0000-7000-8000-0000000000c1");
        var body = new RegisterPayeeRequest("SECONCI", Cnpj, "Unbounded", null, null, null, null);

        var first = await SendWithRequestIdAsync(body, requestId);
        var second = await SendWithRequestIdAsync(body, requestId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var duplicate = await second.Content.ReadFromJsonAsync<PayeeIdResponse>();
        Assert.Equal(Guid.Empty, duplicate!.Id);
        Assert.Equal(1, await ExecuteDbContextAsync(db => db.Payees.CountAsync()));
    }

    // Regressão (auditoria 2026-08-28): a marca de idempotência era só pelo id — o mesmo
    // x-requestid vindo de OUTRO tenant era engolido como duplicata e o cadastro dele não
    // acontecia, em silêncio. A marca é por (tenant, id, comando): cada tenant cadastra o seu.
    [Fact]
    public async Task PostPayee_WithTheSameRequestIdFromAnotherTenant_ShouldRegisterForBoth()
    {
        var requestId = new Guid("0195a1f0-0000-7000-8000-0000000000c2");
        var body = new RegisterPayeeRequest("SECONCI", Cnpj, "Unbounded", null, null, null, null);

        var first = await SendWithRequestIdAsync(body, requestId, TenantId);
        var second = await SendWithRequestIdAsync(body, requestId, OtherTenantId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.NotEqual(Guid.Empty, (await second.Content.ReadFromJsonAsync<PayeeIdResponse>())!.Id);
        Assert.Equal(2, await ExecuteDbContextAsync(db => db.Payees.CountAsync()));
    }

    // Mesmo id em COMANDO diferente não é duplicata: o cadastro e a renomeação com o mesmo
    // x-requestid acontecem os dois.
    [Fact]
    public async Task PutLegalName_WithTheRequestIdOfTheRegistration_ShouldStillRename()
    {
        var requestId = new Guid("0195a1f0-0000-7000-8000-0000000000c3");
        var registered = await SendWithRequestIdAsync(
            new RegisterPayeeRequest("SECONCI", Cnpj, "Unbounded", null, null, null, null), requestId, TenantId);
        var id = (await registered.Content.ReadFromJsonAsync<PayeeIdResponse>())!.Id;

        using var rename = new HttpRequestMessage(HttpMethod.Put, new Uri($"{RouteFor(TenantId)}/{id}/legal-name", UriKind.Relative))
        {
            Content = JsonContent.Create(new RenamePayeeRequest("NOVO NOME")),
        };
        rename.Headers.Add("x-requestid", requestId.ToString());
        var response = await Client.SendAsync(rename);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payee = await ExecuteDbContextAsync(db => db.Payees.AsNoTracking().SingleAsync(p => p.Id == PayeeId.From(id)));
        Assert.Equal("NOVO NOME", payee.LegalName);
    }

    private async Task<HttpResponseMessage> SendWithRequestIdAsync(RegisterPayeeRequest body, Guid requestId, Guid? tenantId = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RouteFor(tenantId ?? TenantId))
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("x-requestid", requestId.ToString());

        return await Client.SendAsync(request);
    }

    private async Task PostAsync(string relativePath, object body)
    {
        var response = await Client.PostAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{relativePath}", UriKind.Relative), body);

        response.EnsureSuccessStatusCode();
    }

    private async Task<Guid> RegisterAsync(RegisterPayeeRequest request, Guid? tenantId = null)
    {
        var response = await Client.PostAsJsonAsync(RouteFor(tenantId ?? TenantId), request);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PayeeIdResponse>();
        return body!.Id;
    }
}
