namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.API.Authorization;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Dublê do cliente UMA: devolve um retrato de permissões que, por padrão, concede TODOS os
/// escopos conhecidos do recurso <c>bill</c> (o comportamento que a suíte sempre teve), e o header
/// de teste <see cref="ScopesHeader"/> restringe ao conjunto listado — é o que permite simular
/// "tem <c>bill:approve</c> mas não <c>approve-danger</c>", impossível com o cliente real (que
/// chamaria o Keycloak) ou com o mock incondicional.
/// </summary>
/// <remarks>
/// A porta de entrada dos endpoints continua sendo decidida pelo <c>MockProtectedResourceHandler</c>;
/// o retrato daqui serve à alçada de risco lida pelo <c>BillsController</c>. Desde 2026-09-04 é UM
/// método só — o cliente real busca todas as permissões de uma vez e o <c>RptCache</c> as guarda.
/// </remarks>
internal sealed class FakeAuthorizationServerClient(IHttpContextAccessor httpContextAccessor)
    : IAuthorizationServerClient
{
    /// <summary>Header com a lista (separada por vírgula) dos escopos que o "usuário" tem.</summary>
    public const string ScopesHeader = "bp_scopes";

    private static readonly string[] AllBillScopes =
    [
        "view", "import", "validate", "approve", "deny", "cancel",
        "approve-attention", "approve-danger", "approve-extreme",
    ];

    public Task<RptFetchResult> FetchAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers[ScopesHeader];

        var scopes = header is null || header.Value.Count == 0
            ? AllBillScopes
            : header.Value
                .SelectMany(v => (v ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToArray();

        var snapshot = RptSnapshot.From([("bill", scopes)]);

        return Task.FromResult(RptFetchResult.Resolved(snapshot));
    }
}
