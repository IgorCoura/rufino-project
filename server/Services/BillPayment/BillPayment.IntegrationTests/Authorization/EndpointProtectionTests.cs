namespace BillPayment.IntegrationTests.Authorization;

using BillPayment.API.Authorization;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Varre TODOS os endpoints registrados e afirma duas coisas que nenhum teste de endpoint pega.
/// </summary>
/// <remarks>
/// <para>
/// <strong>1. Toda rota de tenant declara o parâmetro com o nome <c>tenantId</c>.</strong> O
/// <c>RouteAccessRequirementHandler</c> casa pelo NOME do parâmetro: se um controller novo o
/// batizar de <c>{tenant}</c> ou <c>{id}</c>, o handler acha <c>null</c> e <strong>concede</strong>.
/// O endpoint fica sem guard anti-IDOR, sem erro, sem log, e o teste daquele endpoint passa —
/// porque ele testa a funcionalidade, não a ausência de proteção.
/// </para>
/// <para>
/// <strong>2. Todo endpoint sob <c>api/v1</c> carrega <c>[ProtectedResource]</c>.</strong>
/// Esquecer o atributo deixa o endpoint apenas com a policy padrão — autenticado, mas sem escopo
/// nenhum: quem só lê passa a poder aprovar.
/// </para>
/// <para>
/// Vale para os endpoints de hoje e para os que ainda não existem, que é o ponto: este teste é a
/// única coisa entre um controller novo e um buraco silencioso.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class EndpointProtectionTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string TENANT_ROUTE_PARAM = "tenantId";

    private List<RouteEndpoint> ApiEndpoints()
        => Factory.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("api/v1", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

    // A varredura tem que achar endpoint: uma lista vazia faria os dois testes abaixo passarem à toa.
    [Fact]
    public void ApiEndpoints_ShouldNotBeEmpty()
    {
        Assert.NotEmpty(ApiEndpoints());
    }

    // Toda rota sob api/v1 nomeia o parâmetro de tenant como 'tenantId' — o nome é o que liga o
    // guard anti-IDOR ao endpoint, e um sinônimo o desliga em silêncio.
    [Fact]
    public void EveryApiEndpoint_ShouldDeclareTheTenantRouteParameter()
    {
        var offenders = ApiEndpoints()
            .Where(e => !e.RoutePattern.Parameters.Any(p => string.Equals(p.Name, TENANT_ROUTE_PARAM, StringComparison.Ordinal)))
            .Select(e => e.RoutePattern.RawText)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Rotas sob api/v1 sem o parâmetro '{TENANT_ROUTE_PARAM}' (o guard de tenant não as protege): {string.Join(", ", offenders)}");
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

    // Nenhum endpoint declara escopo vazio: '[ProtectedResource("bill")]' monta a permissão
    // 'bill#' e o servidor de autorização avalia o recurso inteiro, não a ação pedida.
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

    // Endpoint fora de api/v1 que exija autorização precisa ser decisão consciente: hoje o único
    // é a sonda de vida, e ela é [AllowAnonymous].
    [Fact]
    public void HealthEndpoint_ShouldBeAnonymous()
    {
        var health = Factory.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText?.Contains("health", StringComparison.OrdinalIgnoreCase) == true);

        Assert.NotNull(health.Metadata.GetMetadata<IAllowAnonymous>());
    }
}
