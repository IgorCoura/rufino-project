using Microsoft.AspNetCore.Authorization;


namespace PeopleManagement.API.Authorization
{
    public record RouteAccessRequirement(string ParamRouteName, string ClaimType) : IAuthorizationRequirement
    {
    }

    public class RouteAccessRequirementHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<RouteAccessRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;


        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RouteAccessRequirement requirement)
        {
            var parameter = _httpContextAccessor?.HttpContext?.GetRouteValue(requirement.ParamRouteName)?.ToString();
            var claims = context.User.FindAll(x => x.Type == requirement.ClaimType).Select(x => x.Value.ToString()).ToList() ?? [];

            if (claims.Count <= 0)
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
