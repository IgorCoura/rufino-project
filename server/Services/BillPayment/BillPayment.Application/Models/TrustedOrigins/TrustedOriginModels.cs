namespace BillPayment.Application.Models.TrustedOrigins;

using System.Text.Json.Serialization;
using BillPayment.Application.TrustedOrigins.Commands;

/// <summary>
/// Modelos HTTP existem porque o <c>tenantId</c> vem da rota, não do corpo — mandar o
/// tenant no body abriria caminho para divergir do path e virar IDOR.
/// </summary>
/// <remarks>
/// <c>DecidedBy</c> vem do corpo apenas enquanto não há autenticação. Na fase 6, com o
/// Keycloak, passa a sair do claim <c>sub</c> do token (<c>BaseController.GetUserId</c>) e
/// sai do contrato — aceitar o autor da decisão do cliente permitiria forjar a autoria.
/// O <c>[JsonRequired]</c> impede que a omissão vire <c>Guid.Empty</c> silenciosamente.
/// </remarks>
public sealed record RegisterTrustedOriginModel(
    string Kind,
    string Value,
    string Decision,
    [property: JsonRequired] Guid DecidedBy,
    string? Note)
{
    public RegisterTrustedOriginCommand ToCommand(Guid tenantId)
        => new(tenantId, Kind, Value, Decision, DecidedBy, Note);
}

public sealed record ChangeTrustedOriginDecisionModel(
    string Decision,
    [property: JsonRequired] Guid DecidedBy,
    string? Note)
{
    public ChangeTrustedOriginDecisionCommand ToCommand(Guid tenantId, Guid trustedOriginId)
        => new(tenantId, trustedOriginId, Decision, DecidedBy, Note);
}
