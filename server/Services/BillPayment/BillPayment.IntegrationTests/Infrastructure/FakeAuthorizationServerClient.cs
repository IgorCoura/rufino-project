namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.API.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

/// <summary>
/// Dublê do cliente UMA: devolve um retrato de permissões que, por padrão, concede TODOS os
/// escopos conhecidos do recurso <c>bill</c> (o comportamento que a suíte sempre teve), e o header
/// de teste <see cref="ScopesHeader"/> restringe ao conjunto listado — é o que permite simular
/// "tem <c>bill:approve</c> mas não <c>approve-danger</c>", impossível com o cliente real (que
/// chamaria o Keycloak) ou com o mock incondicional.
/// </summary>
/// <remarks>
/// <para>
/// O retrato daqui serve a DOIS consumidores: a alçada de risco lida dentro do
/// <c>BillsController</c> e, desde 2026-09-09, a porta de entrada dos endpoints — o
/// <c>MockProtectedResourceHandler</c> pergunta a este mesmo dublê. Antes disso ele concedia tudo
/// incondicionalmente, e <c>[ProtectedResource]</c> era decorativo na suíte inteira: nenhum teste
/// jamais provou uma NEGATIVA de escopo.
/// </para>
/// <para>
/// Desde 2026-09-04 é UM método só — o cliente real busca todas as permissões de uma vez e o
/// <c>RptCache</c> as guarda.
/// </para>
/// </remarks>
internal sealed class FakeAuthorizationServerClient(IHttpContextAccessor httpContextAccessor)
    : IAuthorizationServerClient
{
    /// <summary>Header com a lista (separada por vírgula) dos escopos que o "usuário" tem.</summary>
    public const string ScopesHeader = "bp_scopes";

    /// <summary>
    /// O único recurso que este dublê modela.
    /// </summary>
    /// <remarks>
    /// O header lista escopos, não pares recurso/escopo, então ele só sabe falar de um recurso.
    /// Quem consulta o retrato precisa saber disso para não recusar um endpoint de
    /// <c>capture-item</c> por causa de uma lista que só descrevia <c>bill</c> — é a constante
    /// que impede a regra de existir escrita à mão em dois lugares.
    /// </remarks>
    public const string ModelledResource = "bill";

    private static readonly string[] AllBillScopes =
    [
        "view", "import", "validate", "approve", "deny", "cancel",
        "approve-attention", "approve-danger", "approve-extreme",
        // O ADR-018 partiu aprovar de agendar e criou estes dois; o dublê ficou para trás e
        // TODA aprovação com data passou a tomar 403 na suíte, porque a guarda de alçada de
        // agendamento consulta este mesmo retrato.
        "schedule", "undo-decision",
    ];

    /// <summary>Os escopos que o header concede — todos, quando ele está ausente.</summary>
    public static string[] ScopesFrom(StringValues header)
        => header.Count == 0
            ? AllBillScopes
            : [.. header.SelectMany(v => (v ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))];

    /// <summary>O retrato que o header descreve, na forma que a produção resolve.</summary>
    public static RptSnapshot SnapshotFrom(StringValues header)
        => RptSnapshot.From([(ModelledResource, ScopesFrom(header))]);

    public Task<RptFetchResult> FetchAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers[ScopesHeader] ?? StringValues.Empty;

        return Task.FromResult(RptFetchResult.Resolved(SnapshotFrom(header)));
    }
}
