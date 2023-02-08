using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Commom.Tests
{
    public class LocalAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {

        /// <summary>
        /// The name of the authorizaton scheme that this handler will respond to.
        /// </summary>
        public const string AuthScheme = "Local";

        public LocalAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        /// <summary>
        /// Marks all authentication requests as successful, and injects the
        /// default company id into the user claims.
        /// </summary>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var sid = Request.Headers["Sid"];
            var role = Request.Headers["Role"];

            var claims = new Claim[]
            {
                new Claim(ClaimTypes.Sid, sid),
                new Claim(ClaimTypes.Role, role)
            };


            var authenticationTicket = new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity( claims , AuthScheme)),
                new AuthenticationProperties(),
                AuthScheme);
            return Task.FromResult(AuthenticateResult.Success(authenticationTicket));
        }
    }
}
