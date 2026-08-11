namespace BillPayment.Domain.TrustedOrigins;

using BillPayment.Domain.SeedWork;

/// <summary>Identidade do <see cref="TrustedOrigin"/>.</summary>
/// <remarks>
/// Comparável para servir de desempate na paginação por keyset — <c>CreatedAt</c> empata em massa
/// e sozinho esconde tudo além da primeira página (a história está em <c>CursorCodec</c>). A ordem
/// que vale é a do <c>uuid</c> no Postgres, onde <c>ORDER BY</c> e <c>WHERE</c> são ambos
/// avaliados; estes operadores existem para a expressão compilar e ser traduzida, e
/// <strong>não</strong> reproduzem a mesma sequência se a coleção for ordenada em memória.
/// </remarks>
public readonly record struct TrustedOriginId(Guid Value)
    : IEntityId<TrustedOriginId>, IComparable<TrustedOriginId>
{
    public static TrustedOriginId New() => new(Guid.CreateVersion7());
    public static TrustedOriginId From(Guid value) => new(value);
    public static TrustedOriginId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();

    public int CompareTo(TrustedOriginId other) => Value.CompareTo(other.Value);

    public static bool operator <(TrustedOriginId left, TrustedOriginId right) => left.CompareTo(right) < 0;
    public static bool operator >(TrustedOriginId left, TrustedOriginId right) => left.CompareTo(right) > 0;
    public static bool operator <=(TrustedOriginId left, TrustedOriginId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(TrustedOriginId left, TrustedOriginId right) => left.CompareTo(right) >= 0;
}
