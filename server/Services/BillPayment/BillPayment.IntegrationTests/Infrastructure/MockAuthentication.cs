namespace BillPayment.IntegrationTests.Infrastructure;

using System.Security.Claims;
using System.Text.Encodings.Web;
using BillPayment.API.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Autenticação de teste: um header <c>Authorization</c> qualquer basta. Sem isto a suíte
/// precisaria de um Keycloak no ar, e passaria a medir se ele está no ar em vez de medir o BC.
/// </summary>
/// <remarks>
/// <strong>O claim <c>sub</c> sai do header <c>x-user-id</c>, e isso é deliberado.</strong> A
/// produção não tem esse caminho — lá quem emite o <c>sub</c> é o Keycloak —, mas os testes que
/// exercitam decisão (aprovar, recusar, reivindicar, dispensar ciclo) precisam dizer QUEM está
/// decidindo, e já diziam por esse header antes de o token existir. Traduzi-lo aqui é o que
/// permitiu remover o fallback do <c>BaseController</c> sem reescrever teste nenhum.
/// </remarks>
public sealed class MockAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthScheme = "Local";
    public const string UserIdHeader = "x-user-id";

    /// <summary>
    /// O claim de tenant deste BC. É o do PRODUTO (<c>bp_tenants</c>), não o <c>tenants</c>
    /// genérico: o genérico diz que a pessoa acessa o tenant, este diz que o tenant contratou o
    /// BillPayment. Tem que casar com <c>Keycloak:RouteClaimTypeRequirement</c> do
    /// <c>appsettings</c> — divergir faria a suíte exercitar um claim que a produção não lê, e
    /// quem guarda isso é <c>RouteGuardTests</c>.
    /// </summary>
    public const string TenantsHeader = "bp_tenants";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(token))
            return Task.FromResult(AuthenticateResult.Fail("Token ausente."));

        var claims = new List<Claim> { new("Token", token) };

        var userId = Request.Headers[UserIdHeader].ToString();
        if (!string.IsNullOrWhiteSpace(userId))
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        // Os tenants viajam no header porque na produção eles viajam no token — o dublê do guard
        // lê deste ClaimsPrincipal, e não do header cru, para exercitar o mesmo caminho.
        var tenants = Request.Headers[TenantsHeader].ToString();
        if (!string.IsNullOrWhiteSpace(tenants))
        {
            foreach (var tenant in tenants.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                claims.Add(new Claim(TenantsHeader, tenant));
        }

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, AuthScheme)),
            new AuthenticationProperties(),
            AuthScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Substitui a ida ao servidor de autorização — quem concede escopo é o realm, e isso não se
/// testa aqui. O guard de rota <strong>não</strong> é substituído: ele é código deste BC, e a
/// policy da suíte usa o <c>RouteAccessRequirementHandler</c> de produção.
/// </summary>
public sealed class MockProtectedResourceHandler : AuthorizationHandler<ProtectedResourceRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProtectedResourceRequirement requirement)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Provedor de policy da suíte. Precisa existir <strong>separado</strong> do de produção por causa
/// do esquema: o de produção exige o JWT Bearer, que não roda aqui, e todo endpoint responderia
/// 401 antes de chegar ao guard.
/// </summary>
/// <remarks>
/// A policy montada é a mesma de produção menos a ida ao Keycloak: exige autenticação e
/// acrescenta o <c>RouteAccessRequirement</c> real. É o que faz os 338 testes existentes
/// atravessarem o guard de verdade em vez de um dublê permissivo.
/// </remarks>
public sealed class MockPolicyProvider(BillPayment.API.Authorization.AuthorizationOptions options) : IAuthorizationPolicyProvider
{
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => Task.FromResult(new AuthorizationPolicyBuilder(MockAuthenticationHandler.AuthScheme)
            .RequireAuthenticatedUser()
            .Build());

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        if (!policyName.StartsWith(ProtectedResourceAttribute.POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<AuthorizationPolicy?>(null);

        var permission = policyName[ProtectedResourceAttribute.POLICY_PREFIX.Length..];

        // O guard sai da MESMA configuração que a produção usa. Fixá-lo aqui faria a suíte
        // continuar verde depois de alguém trocar o claim no appsettings — exercitando um guard
        // que o host não monta.
        var policy = new AuthorizationPolicyBuilder(MockAuthenticationHandler.AuthScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new RouteAccessRequirement(options.RouteNameRequirement, options.RouteClaimTypeRequirement))
            .AddRequirements(new ProtectedResourceRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
