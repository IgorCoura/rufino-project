namespace TenantManagement.API.Authorization;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

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

public class ProtectedResourceRequirementHandler(IHttpContextAccessor httpContextAccessor, IAuthorizationServerClient authorizationServerClient) : AuthorizationHandler<ProtectedResourceRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IAuthorizationServerClient _authorizationServerClient = authorizationServerClient;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProtectedResourceRequirement requirement)
    {
        var permission = requirement.FillPermissionParams(_httpContextAccessor.HttpContext);

        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var result = await _authorizationServerClient.VerifyAccessToResouce(permission, cancellationToken);

        switch (result)
        {
            case ResourceAccessResult.Granted:
                context.Succeed(requirement);
                break;
            case ResourceAccessResult.InvalidToken:
                context.Fail(new InvalidTokenFailureReason(this));
                break;
            case ResourceAccessResult.ServerUnavailable:
                context.Fail(new AuthorizationServerUnavailableFailureReason(this));
                break;
            default:
                context.Fail();
                break;
        }
    }
}
