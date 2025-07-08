using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace PeopleManagement.API.Authorization
{
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

            var success = await _authorizationServerClient.VerifyAccessToResouce(permission);

            if (success)
            {
                context.Succeed(requirement);
                return;
            }
            context.Fail();
        }
    }
}
