using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Commom.Auth.Authorization
{
    public class AuthorizationIdPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly AuthorizationIdOptions _options;

        public AuthorizationIdPolicyProvider(IOptions<AuthorizationIdOptions> options)
        {
            _options = options.Value;
        }

        const string POLICY_PREFIX = AuthorizationIdOptions.POLICY_PREFIX;
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return Task.FromResult(new AuthorizationPolicyBuilder(_options.Schema).RequireAuthenticatedUser().Build());
        }

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder(_options.Schema).RequireAuthenticatedUser().Build());
        }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                var policy = new AuthorizationPolicyBuilder(_options.Schema);
                policy.AddRequirements(new AuthorizationIdRequirement(policyName[POLICY_PREFIX.Length..]));
                return Task.FromResult<AuthorizationPolicy?>(policy.Build());
            }

            return Task.FromResult<AuthorizationPolicy?>(null);
        }
    }
}
