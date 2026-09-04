namespace BillPayment.IntegrationTests.Authorization;

using BillPayment.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

/// <summary>
/// Endpoint sem atributo nasce fechado — sem contêiner, é a policy de produção sendo lida.
/// </summary>
public sealed class FallbackPolicyTests
{
    // Regressão (auditoria 2026-08-28): o fallback era nulo, e um controller ou minimal API
    // esquecido fora de api/v1 — o único prefixo que o teste de erosão varre — nascia público.
    [Fact]
    public async Task GetFallbackPolicy_ShouldRequireAnAuthenticatedUser()
    {
        var provider = new ProtectedResourcePolicyProvider(_ => new AuthorizationPolicyBuilder());

        var policy = await provider.GetFallbackPolicyAsync();

        Assert.NotNull(policy);
        Assert.Contains(policy!.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }
}
