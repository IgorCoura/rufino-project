namespace BillPayment.IntegrationTests.PayerProfiles;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class PayerProfileLifecycleTests : BaseIntegrationTest
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid OtherTenantId = new("0195a1f0-0000-7000-8000-000000000002");

    private const string HeadquartersCnpj = "11222333000181";
    private const string HeadquartersCnpjFormatted = "11.222.333/0001-81";
    private const string BranchCnpj = "11222333000262";
    private const string ForeignCnpj = "11444777000161";
    private const string Cpf = "52998224725";

    private static Uri RouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/payer-profile", UriKind.Relative);

    public PayerProfileLifecycleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Cadastrar uma PJ grava o CNPJ sanitizado e deduz o tipo do documento pelos dígitos.
    [Fact]
    public async Task PostPayerProfile_AsCompany_ShouldPersistSanitizedCnpj()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO EMPREITEIRA LTDA", HeadquartersCnpjFormatted));

        var persisted = await ExecuteDbContextAsync(db => db.PayerProfiles
            .AsNoTracking()
            .SingleAsync(p => p.TenantId == BillPayment.Domain.SharedKernel.TenantId.From(TenantId)));

        Assert.Equal(HeadquartersCnpj, persisted.PrimaryTaxId.Value);
        Assert.Same(TaxIdKind.CNPJ, persisted.PrimaryTaxId.Kind);
        Assert.False(persisted.CanSchedulePayments);
    }

    // PJ com CPF como documento principal é recusada — BLP.PRF02.
    [Fact]
    public async Task PostPayerProfile_AsCompanyWithCpf_ShouldReturnBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            RouteFor(TenantId),
            new RegisterPayerProfileRequest("Company", "RUFINO", Cpf));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PRF02", error!.Id);
    }

    // O cadastro fiscal é um por tenant — o segundo é conflito, BLP.PRF03.
    [Fact]
    public async Task PostPayerProfile_Twice_ShouldReturnConflict()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));

        var response = await Client.PostAsJsonAsync(
            RouteFor(TenantId),
            new RegisterPayerProfileRequest("Company", "OUTRO NOME", ForeignCnpj));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PRF03", error!.Id);
    }

    // Cada tenant tem o seu cadastro — o limite de um é por tenant, não global.
    [Fact]
    public async Task PostPayerProfile_InAnotherTenant_ShouldBeAccepted()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));

        var response = await Client.PostAsJsonAsync(
            RouteFor(OtherTenantId),
            new RegisterPayerProfileRequest("Individual", "IGOR DE BRITO COURA", Cpf));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, await ExecuteDbContextAsync(db => db.PayerProfiles.CountAsync()));
    }

    // Documentos adicionais sobrevivem à ida e volta do banco, com tipo deduzido individualmente.
    [Fact]
    public async Task PostTaxIds_ShouldRoundTripTheAdditionalDocuments()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));

        await PostAsync("tax-ids", new PayerProfileTaxIdRequest("11.222.333/0002-62"));
        await PostAsync("tax-ids", new PayerProfileTaxIdRequest(Cpf));

        var profile = await Client.GetFromJsonAsync<PayerProfileResponse>(RouteFor(TenantId));

        Assert.Equal(2, profile!.AdditionalTaxIds.Count);
        Assert.Contains(profile.AdditionalTaxIds, t => t.Value == BranchCnpj && t.Kind == "CNPJ");
        Assert.Contains(profile.AdditionalTaxIds, t => t.Value == Cpf && t.Kind == "CPF");
    }

    // Remover um documento adicional o retira da coleção persistida.
    [Fact]
    public async Task DeleteTaxId_ShouldRemoveItFromThePersistedCollection()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));
        await PostAsync("tax-ids", new PayerProfileTaxIdRequest(BranchCnpj));

        var response = await Client.DeleteAsync(
            new Uri($"{RouteFor(TenantId)}/tax-ids?taxId={BranchCnpj}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await Client.GetFromJsonAsync<PayerProfileResponse>(RouteFor(TenantId));
        Assert.Empty(profile!.AdditionalTaxIds);
    }

    // O documento principal não pode entrar também como adicional — BLP.PRF09.
    [Fact]
    public async Task PostTaxId_WithPrimaryDocument_ShouldReturnConflict()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));

        var response = await Client.PostAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/tax-ids", UriKind.Relative),
            new PayerProfileTaxIdRequest(HeadquartersCnpjFormatted));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PRF09", error!.Id);
    }

    // Ligar o casamento por raiz de CNPJ persiste a escolha e ela volta na leitura.
    [Fact]
    public async Task PutCnpjRootMatching_ShouldPersistTheChoice()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));

        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/cnpj-root-matching", UriKind.Relative),
            new AlterCnpjRootMatchingRequest(true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await Client.GetFromJsonAsync<PayerProfileResponse>(RouteFor(TenantId));
        Assert.True(profile!.MatchByCnpjRoot);
    }

    // Casar por raiz de CNPJ não faz sentido para pessoa física — BLP.PRF07.
    [Fact]
    public async Task PutCnpjRootMatching_ForIndividual_ShouldReturnBadRequest()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Individual", "IGOR DE BRITO COURA", Cpf));

        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/cnpj-root-matching", UriKind.Relative),
            new AlterCnpjRootMatchingRequest(true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PRF07", error!.Id);
    }

    // Vincular a subconta conclui o onboarding e libera o agendamento; desvincular o revoga.
    [Fact]
    public async Task PutAsaasAccount_ShouldToggleTheAbilityToSchedulePayments()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));

        var linked = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/asaas-account", UriKind.Relative),
            new LinkAsaasAccountRequest("asaas-subaccount-ref-01"));

        var linkedBody = await linked.Content.ReadFromJsonAsync<LinkAsaasAccountResponseContract>();
        Assert.True(linkedBody!.CanSchedulePayments);

        var unlinked = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/asaas-account", UriKind.Relative),
            new LinkAsaasAccountRequest(null));

        var unlinkedBody = await unlinked.Content.ReadFromJsonAsync<LinkAsaasAccountResponseContract>();
        Assert.False(unlinkedBody!.CanSchedulePayments);
    }

    // A referência da subconta não é devolvida na leitura — é ponteiro para segredo.
    [Fact]
    public async Task GetPayerProfile_ShouldNotExposeTheAsaasAccountReference()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));
        await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/asaas-account", UriKind.Relative),
            new LinkAsaasAccountRequest("asaas-subaccount-ref-01"));

        var response = await Client.GetAsync(RouteFor(TenantId));
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("asaas-subaccount-ref-01", raw, StringComparison.Ordinal);
    }

    // Renomear troca a razão social do cadastro persistido.
    [Fact]
    public async Task PutLegalName_ShouldReplaceTheStoredName()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));

        await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/legal-name", UriKind.Relative),
            new RenamePayerProfileRequest("RUFINO EMPREITEIRA LTDA ME"));

        var profile = await Client.GetFromJsonAsync<PayerProfileResponse>(RouteFor(TenantId));
        Assert.Equal("RUFINO EMPREITEIRA LTDA ME", profile!.LegalName);
    }

    // Tenant sem cadastro responde 404 na leitura.
    [Fact]
    public async Task GetPayerProfile_WhenTenantHasNoProfile_ShouldReturnNotFound()
    {
        var response = await Client.GetAsync(RouteFor(TenantId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Alterar cadastro inexistente responde 404 — BLP.PRF04.
    [Fact]
    public async Task PutLegalName_WhenTenantHasNoProfile_ShouldReturnNotFound()
    {
        var response = await Client.PutAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/legal-name", UriKind.Relative),
            new RenamePayerProfileRequest("QUALQUER"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.PRF04", error!.Id);
    }

    // O cadastro de um tenant é invisível para outro: a leitura de fora não o alcança.
    [Fact]
    public async Task GetPayerProfile_FromAnotherTenant_ShouldNotSeeTheProfile()
    {
        await RegisterAsync(new RegisterPayerProfileRequest("Company", "RUFINO", HeadquartersCnpj));

        var response = await Client.GetAsync(RouteFor(OtherTenantId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task PostAsync(string relativePath, object body)
    {
        var response = await Client.PostAsJsonAsync(
            new Uri($"{RouteFor(TenantId)}/{relativePath}", UriKind.Relative), body);

        response.EnsureSuccessStatusCode();
    }

    private async Task RegisterAsync(RegisterPayerProfileRequest request, Guid? tenantId = null)
    {
        var response = await Client.PostAsJsonAsync(RouteFor(tenantId ?? TenantId), request);

        response.EnsureSuccessStatusCode();
    }
}
