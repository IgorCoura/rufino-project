using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using System.Security.Claims;

namespace MaterialPurchase.Tests
{
    public class FakeUserPolicyEvaluator : IPolicyEvaluator
    {
        private ClaimsIdentity _claimsIdentity;
        public virtual async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
        {
            var testScheme = "FakeScheme";
            var principal = new ClaimsPrincipal();
            _claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Sid, "4922766E-D3BA-4D4C-99B0-093D5977D41F"),
                new Claim(ClaimTypes.Role, "15"),

            }, testScheme);

            principal.AddIdentity(_claimsIdentity);
            return await Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal,
                new AuthenticationProperties(), testScheme)));
        }

        public virtual async Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy,
            AuthenticateResult authenticationResult, HttpContext context, object resource)
        {
            context.User = new ClaimsPrincipal(_claimsIdentity);
            return await Task.FromResult(PolicyAuthorizationResult.Success());
        }
    }
}
