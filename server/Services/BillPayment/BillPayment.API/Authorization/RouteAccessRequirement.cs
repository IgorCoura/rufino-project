namespace BillPayment.API.Authorization;

using Microsoft.AspNetCore.Authorization;

public record RouteAccessRequirement(string ParamRouteName, string ClaimType) : IAuthorizationRequirement;

/// <summary>
/// O guard anti-IDOR: o <c>{tenantId}</c> da rota tem que estar no claim <c>tenants</c> do token.
/// </summary>
/// <remarks>
/// <para>
/// A comparação é <strong>case-insensitive</strong>, ao contrário da herdada do PeopleManagement.
/// O parâmetro vem da URL como o cliente a escreveu e o claim vem como o provisionador gravou:
/// um Guid em maiúsculas de um lado e em minúsculas do outro produziria um 403 sem explicação,
/// numa comparação que nada tem de sensível a caixa.
/// </para>
/// <para>
/// Rota <strong>sem</strong> o parâmetro concede — é o que permite endpoint que não é de tenant
/// nenhum. O preço é que um parâmetro batizado errado (<c>{tenant}</c>, <c>{id}</c>) desliga o
/// guard em silêncio; quem impede é o teste de erosão da suíte, que varre o
/// <c>EndpointDataSource</c>.
/// </para>
/// </remarks>
public class RouteAccessRequirementHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<RouteAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RouteAccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

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
