namespace TenantManagement.IntegrationTests.Infrastructure;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantManagement.API.Authorization;

/// <summary>
/// Autenticação de teste: um header <c>Authorization</c> qualquer basta, e o e-mail do
/// "usuário" vem do header <c>x-user-email</c>. Sem isto a suíte precisaria de um Keycloak
/// no ar, e passaria a medir se ele está no ar em vez de medir o BC.
/// </summary>
public sealed class MockAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthScheme = "Local";
    public const string UserEmailHeader = "x-user-email";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(token))
            return Task.FromResult(AuthenticateResult.Fail("Token ausente."));

        var claims = new List<Claim> { new("Token", token) };

        var email = Request.Headers[UserEmailHeader].ToString();
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()));
        }

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, AuthScheme)),
            new AuthenticationProperties(),
            AuthScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Substitui a ida ao servidor de autorização. Continua exercitando o guard de rota — o
/// parâmetro <c>tenantId</c> tem que estar no header <c>tenants</c> —, que é a parte que
/// pertence a este BC; quem concede o escopo é o realm, e isso não se testa aqui.
/// </summary>
public sealed record MockAccessRequirement(string ParamRouteName, string ClaimType) : IAuthorizationRequirement;

public sealed class MockAccessRequirementHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<MockAccessRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MockAccessRequirement requirement)
    {
        var http = httpContextAccessor.HttpContext;
        var token = http?.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(token))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var parameter = http?.GetRouteValue(requirement.ParamRouteName)?.ToString();

        if (string.IsNullOrWhiteSpace(parameter))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var claims = (http?.Request.Headers[requirement.ClaimType].ToString() ?? string.Empty).Split(',');

        if (claims.Contains(parameter, StringComparer.OrdinalIgnoreCase))
            context.Succeed(requirement);
        else
            context.Fail();

        return Task.CompletedTask;
    }
}

/// <summary>
/// Provedor de policy da suíte. Precisa existir <strong>separado</strong> do de produção por
/// causa da policy padrão: a de produção exige o esquema JWT, que não roda aqui, e todo
/// endpoint apenas <c>[Authorize]</c> — o <c>/me/tenants</c> — responderia 401.
/// </summary>
public sealed class MockPolicyProvider : IAuthorizationPolicyProvider
{
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => Task.FromResult(new AuthorizationPolicyBuilder(MockAuthenticationHandler.AuthScheme)
            .RequireAuthenticatedUser()
            .Build());

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(ProtectedResourceAttribute.POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<AuthorizationPolicy?>(null);

        var policy = new AuthorizationPolicyBuilder(MockAuthenticationHandler.AuthScheme)
            .AddRequirements(new MockAccessRequirement("tenantId", "tenants"))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
