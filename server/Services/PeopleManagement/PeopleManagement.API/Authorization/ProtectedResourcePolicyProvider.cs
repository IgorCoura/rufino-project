using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace PeopleManagement.API.Authorization
{
    public class ProtectedResourcePolicyProvider(Func<string, AuthorizationPolicyBuilder> policies) : IAuthorizationPolicyProvider
    {
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return Task.FromResult(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build());
        }

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(ProtectedResourceAttribute.POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                var permission = policyName[ProtectedResourceAttribute.POLICY_PREFIX.Length..];
                var policy = policies.Invoke(permission);
                return Task.FromResult<AuthorizationPolicy?>(policy.Build());
            }

            return Task.FromResult<AuthorizationPolicy?>(null);
        }
    }
}
