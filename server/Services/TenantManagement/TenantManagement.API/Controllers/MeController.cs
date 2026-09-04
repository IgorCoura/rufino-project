namespace TenantManagement.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantManagement.Application.Queries.Tenants;

/// <summary>
/// O que a pessoa autenticada enxerga sobre si mesma. É a consulta que alimenta o seletor de
/// contexto do cliente.
/// </summary>
/// <remarks>
/// Sem <c>[ProtectedResource]</c> de propósito: não há o que autorizar além de estar
/// autenticado — a resposta é construída a partir do e-mail do <strong>próprio token</strong>,
/// nunca de um parâmetro. Aceitar o e-mail por query transformaria o endpoint em oráculo para
/// descobrir a que tenants qualquer pessoa pertence.
/// </remarks>
[Authorize]
[Route("api/v1/me")]
public sealed class MeController(
    ITenantQueries queries,
    ILogger<MeController> logger) : BaseController(logger)
{
    [HttpGet("tenants")]
    public async Task<ActionResult<IReadOnlyList<MyTenantDto>>> ListMyTenants(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
            return OkResponse(Array.Empty<MyTenantDto>());

        return OkResponse(await queries.ListForMemberAsync(email, cancellationToken));
    }
}
