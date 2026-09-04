namespace BillPayment.Application.Models.TrustedOrigins;

using BillPayment.Application.TrustedOrigins.Commands;

/// <summary>
/// Modelos HTTP existem porque o <c>tenantId</c> vem da rota, não do corpo — mandar o
/// tenant no body abriria caminho para divergir do path e virar IDOR.
/// </summary>
/// <remarks>
/// Quem decide vem do <c>sub</c> do token (<c>BaseController.ResolveDecidingUserId</c>), nunca
/// do corpo — aceitar o autor da decisão do cliente permitiria forjar a autoria. O campo
/// <c>DecidedBy</c> existiu no contrato enquanto não havia autenticação e saiu em 2026-08-17,
/// alinhando este agregado com bill/claim/waive. Identidade ausente é recusada pelo domínio
/// (<c>BLP.ORG</c>), não pelo controller, para a regra viver num lugar só.
/// </remarks>
public sealed record RegisterTrustedOriginModel(
    string Kind,
    string Value,
    string Decision,
    string? Note)
{
    public RegisterTrustedOriginCommand ToCommand(Guid tenantId, Guid decidedBy)
        => new(tenantId, Kind, Value, Decision, decidedBy, Note);
}

public sealed record ChangeTrustedOriginDecisionModel(
    string Decision,
    string? Note)
{
    public ChangeTrustedOriginDecisionCommand ToCommand(Guid tenantId, Guid trustedOriginId, Guid decidedBy)
        => new(tenantId, trustedOriginId, Decision, decidedBy, Note);
}
