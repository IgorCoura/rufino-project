using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;

namespace PeopleManagement.API.Authorization
{
    public class AccessTokenPropagationHandler(IHttpContextAccessor contextAccessor, AuthorizationOptions options) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
        private readonly AuthorizationOptions _options = options;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {

            if (_contextAccessor.HttpContext == null)
            {
                return await Continue();
            }

            var httpContext = _contextAccessor.HttpContext;

            var token = httpContext.Request.Headers[HeaderNames.Authorization].ToString().Replace(_options.SourceAuthenticationScheme, "").Trim();

            if (string.IsNullOrEmpty(token) == false)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    _options.SourceAuthenticationScheme,
                    token
                );
            }

            return await Continue();

            Task<HttpResponseMessage> Continue() => base.SendAsync(request, cancellationToken);
        }
    }
}
