namespace BillPayment.API.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

public class ProtectedResourcePolicyProvider(Func<string, AuthorizationPolicyBuilder> policies) : IAuthorizationPolicyProvider
{
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => Task.FromResult(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build());

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => Task.FromResult<AuthorizationPolicy?>(null);

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
