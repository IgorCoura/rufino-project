namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;

/// <summary>
/// A identidade do tenant na plataforma inteira. É este valor que viaja no claim do token
/// e na rota dos produtos — por isso o cadastro aceita um Id informado, e não só um novo:
/// migrar um cadastro que já existe preservando o Id é o que impede reemitir todo o acesso.
/// </summary>
/// <remarks>
/// Comparável para servir de desempate na paginação por keyset: <c>CreatedAt</c> empata — um
/// cadastro em lote carimba o mesmo instante em todos — e sozinho esconderia tudo além da
/// primeira página. A ordem que vale é a do <c>uuid</c> no Postgres, onde <c>ORDER BY</c> e
/// <c>WHERE</c> são ambos avaliados; estes operadores existem para a expressão compilar e ser
/// traduzida, e <strong>não</strong> reproduzem a mesma sequência em memória.
/// </remarks>
public readonly record struct TenantId(Guid Value) : IEntityId<TenantId>, IComparable<TenantId>
{
    public static TenantId New() => new(Guid.CreateVersion7());
    public static TenantId From(Guid value) => new(value);
    public static TenantId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();

    public int CompareTo(TenantId other) => Value.CompareTo(other.Value);

    public static bool operator <(TenantId left, TenantId right) => left.CompareTo(right) < 0;
    public static bool operator >(TenantId left, TenantId right) => left.CompareTo(right) > 0;
    public static bool operator <=(TenantId left, TenantId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(TenantId left, TenantId right) => left.CompareTo(right) >= 0;
}
