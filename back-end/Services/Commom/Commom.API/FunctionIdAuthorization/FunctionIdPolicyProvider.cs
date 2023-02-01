using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.API.FunctionIdAuthorization
{
    public class FunctionIdPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly FunctionIdAuthorizationOptions _options;

        public FunctionIdPolicyProvider(IOptions<FunctionIdAuthorizationOptions> options)
        {
            _options = options.Value;
        }

        const string POLICY_PREFIX = "FunctionId";
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return Task.FromResult(new AuthorizationPolicyBuilder(_options.Schema).RequireAuthenticatedUser().Build());
        }

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                var policy = new AuthorizationPolicyBuilder(_options.Schema);
                policy.AddRequirements(new FunctionIdRequirement(policyName[POLICY_PREFIX.Length..]));
                return Task.FromResult<AuthorizationPolicy?>(policy.Build());
            }

            return Task.FromResult<AuthorizationPolicy?>(null);
        }
    }
}
