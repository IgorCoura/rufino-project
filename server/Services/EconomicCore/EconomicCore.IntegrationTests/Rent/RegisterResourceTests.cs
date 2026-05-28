namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RegisterResourceTests : BaseIntegrationTest
{
    public RegisterResourceTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // POST /resources com payload válido cria EconomicResource e retorna 201 com Id.
    [Fact]
    public async Task PostResource_WhenValidPayload_ShouldReturnCreated()
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/resources",
            new CreateResourceRequest("Apartamento Centro 42", "SERVICE"));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<ResourceResponse>();
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal("Apartamento Centro 42", body.Name);
    }

    // Name vazio dispara ECC.RES01 InvalidName (categoria Validation → 400).
    [Fact]
    public async Task PostResource_WhenNameIsEmpty_ShouldReturnBadRequest()
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/resources",
            new CreateResourceRequest("", "SERVICE"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.RES01", error!.Id);
    }

    // Kind fora do Smart Enum dispara InvalidOperationException no Enumeration.FromDisplayName → 400.
    [Fact]
    public async Task PostResource_WhenKindIsInvalid_ShouldReturnBadRequest()
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/resources",
            new CreateResourceRequest("Apartamento", "INVALID_KIND"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
