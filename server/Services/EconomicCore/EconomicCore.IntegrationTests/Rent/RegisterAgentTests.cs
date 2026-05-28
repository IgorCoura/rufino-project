namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RegisterAgentTests : BaseIntegrationTest
{
    public RegisterAgentTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // POST /agents válido (sem taxId) cria EconomicAgent e retorna 201.
    [Fact]
    public async Task PostAgent_WhenValidPayload_ShouldReturnCreated()
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/agents",
            new CreateAgentRequest("Imobiliária Silva", "OUTSIDE", null, null));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<AgentResponse>();
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal("OUTSIDE", body.Scope);
    }

    // Name vazio dispara ECC.AGT01 InvalidName → 400.
    [Fact]
    public async Task PostAgent_WhenNameIsEmpty_ShouldReturnBadRequest()
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/agents",
            new CreateAgentRequest("", "OUTSIDE", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.AGT01", error!.Id);
    }

    // CPF/CNPJ com dígito verificador inválido dispara SHK.TAX03 InvalidCheckDigit → 400.
    [Fact]
    public async Task PostAgent_WhenTaxIdHasInvalidCheckDigit_ShouldReturnBadRequest()
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/agents",
            new CreateAgentRequest("Imobiliária X", "OUTSIDE", "12345678900", "CPF"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
