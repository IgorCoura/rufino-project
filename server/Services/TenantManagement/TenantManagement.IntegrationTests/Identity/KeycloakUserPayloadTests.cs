namespace TenantManagement.IntegrationTests.Identity;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TenantManagement.Domain.Tenants;
using TenantManagement.Infra.Identity;

/// <summary>
/// O que o adapter escreve na representação de usuário do Keycloak.
/// </summary>
/// <remarks>
/// Não sobe contêiner: o alvo é a montagem do payload, e o provedor é substituído por um
/// <see cref="RecordingHandler"/>. Fica fora da <c>IntegrationTestCollection</c> de propósito.
/// </remarks>
public sealed class KeycloakUserPayloadTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // REGRESSÃO: o handler passava o nome do TENANT como displayName e o adapter o gravava em
    // `firstName`. O titular aparecia no Keycloak chamando-se "Padaria do Zé LTDA".
    [Fact]
    public async Task Criar_pessoa_nao_inventa_nome_para_ela()
    {
        var (provisioner, handler) = Build();

        await provisioner.GrantAccessAsync(TenantId.From(Tenant), "dono@paoquente.com.br");

        var created = handler.CreatedUser;
        Assert.NotNull(created);
        Assert.False(created!.Value.TryGetProperty("firstName", out var first) && first.ValueKind != JsonValueKind.Null,
            "o adapter não pode inventar nome de pessoa — o único nome que este BC conhece é o do tenant");
        Assert.Equal("dono@paoquente.com.br", created.Value.GetProperty("email").GetString());
        Assert.Equal("dono@paoquente.com.br", created.Value.GetProperty("username").GetString());
    }

    // O convite pede o nome à própria pessoa: sem UPDATE_PROFILE ela entraria sem nome nenhum.
    [Fact]
    public async Task Convite_pede_que_a_pessoa_informe_o_proprio_nome()
    {
        var (provisioner, handler) = Build();

        await provisioner.GrantAccessAsync(TenantId.From(Tenant), "dono@paoquente.com.br");

        var actions = handler.CreatedUser!.Value
            .GetProperty("requiredActions")
            .EnumerateArray()
            .Select(a => a.GetString())
            .ToList();

        Assert.Contains("UPDATE_PROFILE", actions);
        Assert.Contains("UPDATE_PASSWORD", actions);
        Assert.Contains("VERIFY_EMAIL", actions);
    }

    // O tenant vai no atributo que o mapper do realm expõe como claim.
    [Fact]
    public async Task Criar_pessoa_ja_leva_o_tenant_no_atributo()
    {
        var (provisioner, handler) = Build();

        await provisioner.GrantAccessAsync(TenantId.From(Tenant), "dono@paoquente.com.br");

        var tenants = handler.CreatedUser!.Value
            .GetProperty("attributes")
            .GetProperty("tenants")
            .EnumerateArray()
            .Select(t => t.GetString())
            .ToList();

        Assert.Equal([Tenant.ToString()], tenants);
    }

    // Pessoa que já existe conserva o que o provedor tinha dela — inclusive o nome que ela
    // mesma informou e o `companies` de que o PeopleManagement depende.
    [Fact]
    public async Task Pessoa_existente_conserva_nome_e_demais_atributos()
    {
        var (provisioner, handler) = Build(existingUser: """
            {
              "id": "0f9c1d3a-5b6e-4c7d-8a9b-0c1d2e3f4a5b",
              "username": "dono@paoquente.com.br",
              "email": "dono@paoquente.com.br",
              "firstName": "José",
              "enabled": true,
              "attributes": { "companies": ["c0ffee00-0000-0000-0000-000000000000"] }
            }
            """);

        await provisioner.GrantAccessAsync(TenantId.From(Tenant), "dono@paoquente.com.br");

        var updated = handler.UpdatedUser;
        Assert.NotNull(updated);
        Assert.Equal("José", updated!.Value.GetProperty("firstName").GetString());
        Assert.Single(updated.Value.GetProperty("attributes").GetProperty("companies").EnumerateArray());
        Assert.Equal(
            Tenant.ToString(),
            updated.Value.GetProperty("attributes").GetProperty("tenants")[0].GetString());
        Assert.Null(handler.CreatedUser);
    }

    private static (KeycloakTenantAccessProvisioner Provisioner, RecordingHandler Handler) Build(
        string? existingUser = null)
    {
        var handler = new RecordingHandler(existingUser);
        var client = new HttpClient(handler);
        var options = Options.Create(new TenantProvisioningOptions
        {
            Enabled = true,
            AuthServerUrl = "https://keycloak.example.com",
            Realm = "rufino",
            ClientId = "tenant-management-provisioner",
            ClientSecret = "segredo",
            InvitationClientId = "rufino-app",
        });

        return (
            new KeycloakTenantAccessProvisioner(
                client,
                options,
                TimeProvider.System,
                NullLogger<KeycloakTenantAccessProvisioner>.Instance),
            handler);
    }

    /// <summary>Keycloak de mentira: responde o mínimo e guarda o que recebeu.</summary>
    private sealed class RecordingHandler(string? existingUser) : HttpMessageHandler
    {
        private bool _created;

        public JsonElement? CreatedUser { get; private set; }

        public JsonElement? UpdatedUser { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (path.EndsWith("/token", StringComparison.Ordinal))
                return Json("""{"access_token":"token-de-mentira","expires_in":300}""");

            if (request.Method == HttpMethod.Get && path.EndsWith("/users", StringComparison.Ordinal))
            {
                if (existingUser is not null)
                    return Json($"[{existingUser}]");

                // Antes de criar não existe ninguém; depois, o adapter relê para pegar o id.
                return Json(_created
                    ? """[{"id":"0f9c1d3a-5b6e-4c7d-8a9b-0c1d2e3f4a5b","email":"dono@paoquente.com.br"}]"""
                    : "[]");
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("/users", StringComparison.Ordinal))
            {
                CreatedUser = await ReadBodyAsync(request, cancellationToken);
                _created = true;
                return new HttpResponseMessage(HttpStatusCode.Created);
            }

            if (request.Method == HttpMethod.Put && path.Contains("/users/", StringComparison.Ordinal)
                && !path.EndsWith("execute-actions-email", StringComparison.Ordinal))
            {
                UpdatedUser = await ReadBodyAsync(request, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private static async Task<JsonElement> ReadBodyAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonDocument.Parse(body).RootElement.Clone();
        }

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            };
    }
}
