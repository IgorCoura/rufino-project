using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


namespace PeopleManagement.API.Authorization
{
    public record RouteAccessRequirement(string ParamRouteName, string ClaimType) : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// O guard anti-IDOR: o <c>{company}</c> da rota tem que estar no claim <c>pm_tenants</c> do token.
    /// </summary>
    /// <remarks>
    /// A comparação do valor é <strong>case-insensitive</strong>. O parâmetro vem da URL como o
    /// cliente a escreveu e o claim vem como o provisionador do TenantManagement gravou: um Guid
    /// em maiúsculas de um lado e em minúsculas do outro produziria um 403 sem explicação, numa
    /// comparação que nada tem de sensível a caixa. Mesma correção já aplicada no BillPayment.
    /// </remarks>
    public class RouteAccessRequirementHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<RouteAccessRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;


        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RouteAccessRequirement requirement)
        {
            var parameter = _httpContextAccessor?.HttpContext?.GetRouteValue(requirement.ParamRouteName)?.ToString();

            if (string.IsNullOrWhiteSpace(parameter))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var claims = context.User
                .FindAll(x => x.Type.Contains(requirement.ClaimType, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value)
                .ToList();

            if (claims.Exists(c => string.Equals(c, parameter, StringComparison.OrdinalIgnoreCase)))
                context.Succeed(requirement);
            else
                context.Fail();

            return Task.CompletedTask;
        }
    }
}
