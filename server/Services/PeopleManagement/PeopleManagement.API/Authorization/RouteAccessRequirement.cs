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
    /// <para>
    /// A comparação do valor é <strong>case-insensitive</strong>. O parâmetro vem da URL como o
    /// cliente a escreveu e o claim vem como o provisionador do TenantManagement gravou: um Guid
    /// em maiúsculas de um lado e em minúsculas do outro produziria um 403 sem explicação, numa
    /// comparação que nada tem de sensível a caixa.
    /// </para>
    /// <para>
    /// O <strong>tipo</strong> do claim casa por igualdade exata desde 2026-09-04. Antes era
    /// <c>Contains</c>, e com ele <c>"bp_tenants".Contains("tenants")</c> é verdadeiro: uma API
    /// configurada para ler o <c>tenants</c> genérico aceitaria também os valores do claim de
    /// outro produto. O sentido que nos protegia era acidente de nomenclatura, não desenho — e a
    /// próxima API a se chamar <c>&lt;sigla&gt;_tenants</c> reabriria o buraco.
    /// </para>
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
                .FindAll(x => string.Equals(x.Type, requirement.ClaimType, StringComparison.OrdinalIgnoreCase))
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
