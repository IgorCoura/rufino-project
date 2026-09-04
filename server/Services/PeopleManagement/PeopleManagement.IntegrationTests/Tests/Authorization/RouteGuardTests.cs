using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.IntegrationTests.Configs;
using AuthorizationOptions = PeopleManagement.API.Authorization.AuthorizationOptions;

namespace PeopleManagement.IntegrationTests.Tests.Authorization
{
    /// <summary>
    /// O guard de rota (anti-IDOR) pela borda HTTP: sem token 401, empresa fora do claim 403,
    /// empresa dentro 200 — e a caixa do Guid não pode importar.
    /// </summary>
    /// <remarks>
    /// Até 2026-09-04 este BC não tinha teste nenhum do guard: a suíte trocava a policy por um
    /// <c>MockAccessRequirement("company", "companies")</c> escrito à mão, com o claim LEGADO.
    /// Trocar o claim no <c>appsettings</c> não quebrava teste algum, e o guard exercitado não era
    /// o que o deploy monta. Agora a fábrica lê o nome do parâmetro e o do claim do mesmo
    /// <c>AuthorizationOptions</c> da produção, e o primeiro teste desta classe é o que impede a
    /// divergência voltar.
    /// </remarks>
    [Collection(nameof(IntegrationTestCollection))]
    public sealed class RouteGuardTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        // O claim que a produção lê tem que ser o mesmo que a suíte envia — senão todo teste desta
        // suíte exercita um guard que o deploy não produz.
        [Fact]
        public void OGuardDeRota_TemQueLerOMesmoClaimQueASuiteEnvia()
        {
            var options = _factory.Services.GetRequiredService<AuthorizationOptions>();

            Assert.Equal(ConfigsUtils.TENANT_CLAIM_HEADER, options.RouteClaimTypeRequirement);
            Assert.Equal("company", options.RouteNameRequirement);
        }

        // Requisição sem token nenhum é 401, não 403: falta de autenticação e falta de permissão
        // pedem reações diferentes do cliente (relogar × pedir acesso).
        [Fact]
        public async Task SemToken_DeveResponder401()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync($"api/v1/{Guid.NewGuid()}/department/all");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // Empresa que não está no claim é 403 — é o guard anti-IDOR fazendo seu trabalho.
        [Fact]
        public async Task EmpresaForaDoClaim_DeveResponder403()
        {
            var client = _factory.CreateClient().InputHeaders([Guid.NewGuid()]);

            var response = await client.GetAsync($"api/v1/{Guid.NewGuid()}/department/all");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // Empresa presente no claim atravessa o guard (o desfecho do endpoint em si não importa aqui,
        // só que ele não parou em 401/403).
        [Fact]
        public async Task EmpresaNoClaim_DeveAtravessarOGuard()
        {
            var company = Guid.NewGuid();
            var client = _factory.CreateClient().InputHeaders([company]);

            var response = await client.GetAsync($"api/v1/{company}/department/all");

            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // O MESMO Guid em maiúsculas continua passando: o parâmetro vem da URL como o cliente
        // escreveu e o claim como o provisionador gravou. Comparar com sensibilidade a caixa
        // produzia 403 sem explicação, numa comparação que nada tem de sensível a caixa.
        [Fact]
        public async Task EmpresaNoClaimComCaixaDiferente_DeveAtravessarOGuard()
        {
            var company = Guid.NewGuid();
            var client = _factory.CreateClient().InputHeaders([company]);

            var response = await client.GetAsync($"api/v1/{company.ToString().ToUpperInvariant()}/department/all");

            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // A sonda de vida responde sem token: é o que permite saber se a API está de pé quando o
        // Keycloak não está.
        [Fact]
        public async Task SondaDeVida_DeveResponderSemToken()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("api/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
