namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.API.Authorization;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Dublê do cliente UMA: por padrão concede TODOS os escopos pedidos (o comportamento que a
/// suíte sempre teve), e o header de teste <see cref="ScopesHeader"/> restringe ao conjunto
/// listado — é o que permite simular "tem <c>bill:approve</c> mas não <c>approve-danger</c>",
/// impossível com o cliente real (que chamaria o Keycloak) ou com o mock incondicional.
/// </summary>
internal sealed class FakeAuthorizationServerClient(IHttpContextAccessor httpContextAccessor)
    : IAuthorizationServerClient
{
    /// <summary>Header com a lista (separada por vírgula) dos escopos que o "usuário" tem.</summary>
    public const string ScopesHeader = "bp_scopes";

    // A porta de entrada dos endpoints já é decidida pelo MockProtectedResourceHandler; este
    // caminho só existe para satisfazer a interface.
    public Task<ResourceAccessResult> VerifyAccessToResouce(
        string permission, CancellationToken cancellationToken = default)
        => Task.FromResult(ResourceAccessResult.Granted);

    public Task<IReadOnlyCollection<string>> GetGrantedScopesAsync(
        string resource,
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken = default)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers[ScopesHeader];

        if (header is null || header.Value.Count == 0)
            return Task.FromResult<IReadOnlyCollection<string>>(scopes.ToList());

        var allowed = header.Value
            .SelectMany(v => (v ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.Ordinal);

        return Task.FromResult<IReadOnlyCollection<string>>(scopes.Where(allowed.Contains).ToList());
    }
}
