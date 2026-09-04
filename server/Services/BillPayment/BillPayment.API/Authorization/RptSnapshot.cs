namespace BillPayment.API.Authorization;

using System.Collections.Frozen;

/// <summary>
/// Retrato de TODAS as permissões que o servidor de autorização concede a um token, num instante.
/// </summary>
/// <remarks>
/// <para>
/// É o coração da Opção 2 do plano de 2026-09-04: em vez de uma pergunta UMA por requisição
/// (<c>"esta pessoa tem bill#approve?"</c>), uma pergunta por TOKEN (<c>"o que esta pessoa
/// tem?"</c>), respondida com <c>response_mode=permissions</c> e sem o parâmetro
/// <c>permission</c> — o Keycloak devolve a lista inteira. Todo endpoint seguinte resolve em
/// memória.
/// </para>
/// <para>
/// O ganho não é só latência: o <c>approve</c> fazia DUAS idas ao Keycloak (a policy do endpoint e
/// a alçada de risco). Com o retrato, as duas leem a mesma lista, e a alçada sai de graça.
/// </para>
/// <para>
/// <strong>Um retrato vazio é resposta válida</strong>, não erro: é o que o servidor devolve para
/// quem está autenticado e não tem permissão nenhuma. Quem distingue "sem permissão" de "não
/// consegui perguntar" é o <see cref="RptFetchOutcome"/>, não este tipo.
/// </para>
/// </remarks>
public sealed class RptSnapshot
{
    private readonly FrozenDictionary<string, FrozenSet<string>> _byResource;

    private RptSnapshot(FrozenDictionary<string, FrozenSet<string>> byResource)
    {
        _byResource = byResource;
    }

    public static RptSnapshot Empty { get; } = new(FrozenDictionary<string, FrozenSet<string>>.Empty);

    public static RptSnapshot From(IEnumerable<(string Resource, IReadOnlyCollection<string> Scopes)> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        // O servidor pode repetir o mesmo recurso em entradas diferentes (uma por permissão que
        // casou). Unir em vez de sobrescrever: sobrescrever perderia escopo concedido por outra
        // permissão e produziria um 403 que nenhuma configuração do realm explica.
        var accumulator = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var (resource, scopes) in permissions)
        {
            if (string.IsNullOrWhiteSpace(resource))
                continue;

            if (!accumulator.TryGetValue(resource, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                accumulator[resource] = set;
            }

            foreach (var scope in scopes ?? [])
                set.Add(scope);
        }

        return new RptSnapshot(accumulator.ToFrozenDictionary(
            pair => pair.Key,
            pair => pair.Value.ToFrozenSet(StringComparer.Ordinal),
            StringComparer.Ordinal));
    }

    /// <summary>
    /// Resolve uma permissão no formato <c>recurso#escopo1,escopo2</c> contra o retrato.
    /// </summary>
    /// <remarks>
    /// Sem escopo nenhum (<c>recurso#</c>), a pergunta é sobre o recurso inteiro: basta ele estar
    /// no retrato. Com escopos, o modo de validação decide entre exigir todos ou qualquer um —
    /// exatamente o que o <c>ScopesValidationMode</c> significava quando quem avaliava era o
    /// servidor.
    /// </remarks>
    public bool Grants(string permission, ScopesValidationMode mode)
    {
        ArgumentNullException.ThrowIfNull(permission);

        var separator = permission.IndexOf('#', StringComparison.Ordinal);
        var resource = separator < 0 ? permission : permission[..separator];
        var scopeExpression = separator < 0 ? string.Empty : permission[(separator + 1)..];

        if (!_byResource.TryGetValue(resource, out var granted))
            return false;

        var scopes = scopeExpression.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (scopes.Length == 0)
            return true;

        return mode == ScopesValidationMode.AnyOf
            ? Array.Exists(scopes, granted.Contains)
            : Array.TrueForAll(scopes, granted.Contains);
    }

    /// <summary>Quais dos escopos pedidos o retrato concede sobre o recurso. Base da alçada de risco.</summary>
    public IReadOnlyCollection<string> GrantedScopes(string resource, IEnumerable<string> candidates)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(candidates);

        return _byResource.TryGetValue(resource, out var granted)
            ? candidates.Where(granted.Contains).ToList()
            : [];
    }
}

/// <summary>Desfecho de uma tentativa de obter o retrato de permissões.</summary>
public enum RptFetchOutcome
{
    /// <summary>O servidor respondeu. O retrato pode estar vazio — vazio é resposta, não falha.</summary>
    Resolved,

    /// <summary>O servidor recusou o próprio token (expirado, revogado, not-before).</summary>
    InvalidToken,

    /// <summary>O servidor não foi alcançado ou respondeu com erro de servidor.</summary>
    Unavailable,
}

public readonly record struct RptFetchResult(RptFetchOutcome Outcome, RptSnapshot Snapshot)
{
    public static RptFetchResult Resolved(RptSnapshot snapshot) => new(RptFetchOutcome.Resolved, snapshot);

    public static RptFetchResult InvalidToken() => new(RptFetchOutcome.InvalidToken, RptSnapshot.Empty);

    public static RptFetchResult Unavailable() => new(RptFetchOutcome.Unavailable, RptSnapshot.Empty);
}
