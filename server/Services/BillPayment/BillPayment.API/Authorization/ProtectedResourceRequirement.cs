namespace BillPayment.API.Authorization;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

public partial record ProtectedResourceRequirement(string Permission) : IAuthorizationRequirement
{
    public string FillPermissionParams(HttpContext? httpContext)
    {
        if (httpContext is null)
            return Permission;

        var pathParameters = httpContext.GetRouteData()?.Values;

        if (pathParameters is not null)
        {
            Regex paramRegex = ParamRegex();

            return paramRegex.Replace(Permission, m =>
            {
                string key = m.Groups[1].Value;
                return pathParameters!.GetValueOrDefault(key)?.ToString() ?? "";
            });
        }

        return Permission;
    }

    [GeneratedRegex("{(.*?)}")]
    private static partial Regex ParamRegex();
}

/// <summary>
/// Resolve a permissão do endpoint contra o retrato de permissões do token.
/// </summary>
/// <remarks>
/// Desde 2026-09-04 não fala com o Keycloak: pede o retrato ao <see cref="IRptCache"/>, que o busca
/// UMA vez por token. A tradução de desfecho continua idêntica — token recusado vira 401 e servidor
/// fora do ar vira 503, em vez de os dois colapsarem no 403 padrão.
/// </remarks>
public class ProtectedResourceRequirementHandler(
    IHttpContextAccessor httpContextAccessor,
    IRptCache rptCache,
    AuthorizationOptions options) : AuthorizationHandler<ProtectedResourceRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRptCache _rptCache = rptCache;
    private readonly AuthorizationOptions _options = options;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProtectedResourceRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var permission = requirement.FillPermissionParams(_httpContextAccessor.HttpContext);

        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var result = await _rptCache.GetAsync(cancellationToken);

        switch (result.Outcome)
        {
            case RptFetchOutcome.Resolved:
                if (result.Snapshot.Grants(permission, _options.ScopesValidationMode))
                    context.Succeed(requirement);
                else
                    context.Fail();
                break;
            case RptFetchOutcome.InvalidToken:
                context.Fail(new InvalidTokenFailureReason(this));
                break;
            default:
                context.Fail(new AuthorizationServerUnavailableFailureReason(this));
                break;
        }
    }
}
