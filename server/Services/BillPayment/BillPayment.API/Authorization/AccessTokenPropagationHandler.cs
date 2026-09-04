namespace BillPayment.API.Authorization;

using System.Net.Http.Headers;
using Microsoft.Net.Http.Headers;

/// <summary>
/// Repassa o token de quem chamou para o servidor de autorização. É ele que torna a checagem
/// UMA uma pergunta sobre <em>aquela pessoa</em>, e não sobre o serviço.
/// </summary>
public class AccessTokenPropagationHandler(IHttpContextAccessor contextAccessor, AuthorizationOptions options) : DelegatingHandler
{
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
    private readonly AuthorizationOptions _options = options;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_contextAccessor.HttpContext == null)
        {
            return await Continue();
        }

        var httpContext = _contextAccessor.HttpContext;

        var token = httpContext.Request.Headers[HeaderNames.Authorization].ToString()
            .Replace(_options.SourceAuthenticationScheme, "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(_options.SourceAuthenticationScheme, token);
        }

        return await Continue();

        Task<HttpResponseMessage> Continue() => base.SendAsync(request, cancellationToken);
    }
}
