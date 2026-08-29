namespace BillPayment.API.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

public class ProtectedResourcePolicyProvider(Func<string, AuthorizationPolicyBuilder> policies) : IAuthorizationPolicyProvider
{
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => Task.FromResult(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build());

    // Fallback = exige autenticação: endpoint sem [Authorize]/[ProtectedResource] nasce FECHADO,
    // não aberto. Até 2026-08-28 era nulo — um controller ou minimal API esquecido fora de
    // `api/v1` (que é o único prefixo que o teste de erosão varre) ficaria público. Quem precisa
    // ser anônimo declara [AllowAnonymous] (o health) ou .AllowAnonymous() (o OpenAPI em dev).
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => Task.FromResult<AuthorizationPolicy?>(
            new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build());

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        if (policyName.StartsWith(ProtectedResourceAttribute.POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[ProtectedResourceAttribute.POLICY_PREFIX.Length..];
            var policy = policies.Invoke(permission);
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return Task.FromResult<AuthorizationPolicy?>(null);
    }
}
