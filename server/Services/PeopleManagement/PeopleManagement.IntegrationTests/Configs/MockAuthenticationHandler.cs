using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PeopleManagement.IntegrationTests.Configs
{
    public class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>
        /// The name of the authorizaton scheme that this handler will respond to.
        /// </summary>
        public const string AuthScheme = "Local";

        public MockAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        /// <summary>
        /// Marks all authentication requests as successful, and injects the
        /// default company id into the user claims.
        /// </summary>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = Request.Headers.Authorization;

            if(string.IsNullOrEmpty(token))
            {
                return Task.FromResult(AuthenticateResult.Fail("O token invalido."));
            }

            var authenticationTicket = new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim("Token", token!)])),
                new AuthenticationProperties(),
                AuthScheme);

            return Task.FromResult(AuthenticateResult.Success(authenticationTicket));
        }
    }

    public record MockAccessRequirement(string ParamRouteName, string ClaimType) : IAuthorizationRequirement
    {
    }

    public class MockAccessRequirementHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<MockAccessRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;


        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MockAccessRequirement requirement)
        {
            var token = _httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization].ToString();

            if (string.IsNullOrEmpty(token))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var parameter = _httpContextAccessor?.HttpContext?.GetRouteValue(requirement.ParamRouteName)?.ToString();
            var claimsString = _httpContextAccessor?.HttpContext?.Request.Headers[requirement.ClaimType].ToString() ?? string.Empty;  
            var claims = claimsString.Split(',');

            if (claims.Length <= 0)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (string.IsNullOrWhiteSpace(parameter) || claims.Contains(parameter))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            context.Fail();
            return Task.CompletedTask;
        }
    }
}

