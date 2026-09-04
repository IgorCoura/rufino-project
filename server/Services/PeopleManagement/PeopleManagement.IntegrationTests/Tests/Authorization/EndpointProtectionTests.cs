using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.API.Authorization;
using PeopleManagement.IntegrationTests.Configs;

namespace PeopleManagement.IntegrationTests.Tests.Authorization
{
    /// <summary>
    /// Varre TODOS os endpoints registrados e afirma o que nenhum teste de endpoint pega.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>1. Toda rota de empresa declara o parâmetro com o nome <c>company</c>.</strong> O
    /// <c>RouteAccessRequirementHandler</c> casa pelo NOME do parâmetro: se um controller novo o
    /// batizar de <c>{companyId}</c> ou <c>{id}</c>, o handler acha <c>null</c> e
    /// <strong>concede</strong>. O endpoint fica sem guard anti-IDOR, sem erro, sem log, e o teste
    /// daquele endpoint passa — porque ele testa a funcionalidade, não a ausência de proteção.
    /// </para>
    /// <para>
    /// <strong>2. Todo endpoint sob <c>api/v1</c> carrega <c>[ProtectedResource]</c> com escopo.</strong>
    /// Sem o atributo sobra só a policy de fallback — autenticado, mas sem escopo nenhum: quem só lê
    /// passa a poder editar.
    /// </para>
    /// <para>
    /// Vale para os endpoints de hoje e para os que ainda não existem, que é o ponto: este teste é a
    /// única coisa entre um controller novo e um buraco silencioso. Portado do BillPayment em
    /// 2026-09-04.
    /// </para>
    /// </remarks>
    [Collection(nameof(IntegrationTestCollection))]
    public sealed class EndpointProtectionTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        private const string COMPANY_ROUTE_PARAM = "company";

        /// <summary>
        /// Rotas conscientemente fora do guard de empresa, cada uma com o motivo. Entrar aqui é
        /// decisão explícita e revisável — que é o oposto de esquecer o parâmetro e não descobrir.
        /// </summary>
        private static readonly Dictionary<string, string> KnownUnguardedRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["api/v1/Company"] = "Cadastro de empresa: a rota não tem {company} porque ela é sobre a "
                                 + "própria empresa. Os três GET recebem o id por query string e NÃO o "
                                 + "validam contra o claim — é o achado A1 da auditoria de 2026-09-04, "
                                 + "deixado fora daquele plano por decisão do usuário. Ao fechá-lo, "
                                 + "remova esta entrada.",
            ["api/v1/Company/list"] = "Idem api/v1/Company.",
            ["api/v1/Company/complete"] = "Idem api/v1/Company.",
            ["api/v1/Document/webhook"] = "Retorno do provedor de assinatura: quem chama é o ZapSign, "
                                          + "que não conhece empresa nenhuma e não teria o que pôr em "
                                          + "{company}. A proteção aqui é o escopo document:webhook, "
                                          + "concedido só ao papel de webhook de assinatura, que vive "
                                          + "numa service account.",
        };

        private List<RouteEndpoint> ApiEndpoints()
            => _factory.Services.GetRequiredService<EndpointDataSource>()
                .Endpoints
                .OfType<RouteEndpoint>()
                .Where(e => e.RoutePattern.RawText?.StartsWith("api/v1", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

        // A varredura tem que achar endpoint: uma lista vazia faria os testes abaixo passarem à toa.
        [Fact]
        public void ApiEndpoints_ShouldNotBeEmpty()
        {
            Assert.NotEmpty(ApiEndpoints());
        }

        // Toda rota sob api/v1 nomeia o parâmetro de empresa como 'company' — o nome é o que liga o
        // guard anti-IDOR ao endpoint, e um sinônimo o desliga em silêncio.
        [Fact]
        public void EveryApiEndpoint_ShouldDeclareTheCompanyRouteParameter()
        {
            var offenders = ApiEndpoints()
                .Where(e => !e.RoutePattern.Parameters.Any(p => string.Equals(p.Name, COMPANY_ROUTE_PARAM, StringComparison.Ordinal)))
                .Select(e => e.RoutePattern.RawText ?? string.Empty)
                .Where(route => !KnownUnguardedRoutes.ContainsKey(route))
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"Rotas sob api/v1 sem o parâmetro '{COMPANY_ROUTE_PARAM}' (o guard de empresa não as protege): {string.Join(", ", offenders)}");
        }

        // A lista de exceções não pode envelhecer: rota que saiu do código tem que sair dela também,
        // senão a próxima rota com o mesmo caminho herda uma dispensa que ninguém revisou.
        [Fact]
        public void KnownUnguardedRoutes_ShouldAllStillExist()
        {
            var routes = ApiEndpoints().Select(e => e.RoutePattern.RawText ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var stale = KnownUnguardedRoutes.Keys.Where(route => !routes.Contains(route)).ToList();

            Assert.True(
                stale.Count == 0,
                $"Exceções declaradas para rotas que não existem mais — remova de KnownUnguardedRoutes: {string.Join(", ", stale)}");
        }

        // Todo endpoint sob api/v1 declara recurso e escopo — sem o atributo, sobra só "autenticado".
        [Fact]
        public void EveryApiEndpoint_ShouldCarryAProtectedResourceAttribute()
        {
            var offenders = ApiEndpoints()
                .Where(e => e.Metadata.GetMetadata<ProtectedResourceAttribute>() is null)
                .Select(e => e.RoutePattern.RawText)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"Endpoints sob api/v1 sem [ProtectedResource]: {string.Join(", ", offenders)}");
        }

        // Nenhum endpoint declara escopo vazio: '[ProtectedResource("employee")]' monta a permissão
        // 'employee#' e o servidor de autorização avalia o recurso inteiro, não a ação pedida.
        [Fact]
        public void NoApiEndpoint_ShouldDeclareAnEmptyScope()
        {
            var offenders = ApiEndpoints()
                .Where(e => e.Metadata.GetMetadata<ProtectedResourceAttribute>() is { } attr
                         && string.IsNullOrWhiteSpace(attr.GetScopesExpression()))
                .Select(e => e.RoutePattern.RawText)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"Endpoints com [ProtectedResource] sem escopo: {string.Join(", ", offenders)}");
        }

        // A sonda de vida é a ÚNICA rota anônima da API, e é anônima de propósito: um health check
        // que exige token deixa de responder exatamente quando o Keycloak cai.
        [Fact]
        public void HealthEndpoint_ShouldBeAnonymous()
        {
            var health = _factory.Services.GetRequiredService<EndpointDataSource>()
                .Endpoints
                .OfType<RouteEndpoint>()
                .Single(e => e.RoutePattern.RawText?.Contains("health", StringComparison.OrdinalIgnoreCase) == true);

            Assert.NotNull(health.Metadata.GetMetadata<IAllowAnonymous>());
        }
    }
}
