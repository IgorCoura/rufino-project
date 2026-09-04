namespace BillPayment.Domain.CapturedMessages;

using BillPayment.Domain.SeedWork;

/// <summary>Identidade do <see cref="CapturedMessage"/>.</summary>
/// <remarks>
/// Comparável para servir de desempate na paginação por keyset — <c>ReceivedAt</c> empata em
/// massa (uma varredura carimba um instante só) e sozinho esconde tudo além da primeira página.
/// A ordem que vale é a do <c>uuid</c> no Postgres, onde <c>ORDER BY</c> e <c>WHERE</c> são ambos
/// avaliados; estes operadores existem para a expressão compilar e ser traduzida, e
/// <strong>não</strong> reproduzem a mesma sequência se a coleção for ordenada em memória.
/// </remarks>
public readonly record struct CapturedMessageId(Guid Value)
    : IEntityId<CapturedMessageId>, IComparable<CapturedMessageId>
{
    public static CapturedMessageId New() => new(Guid.CreateVersion7());
    public static CapturedMessageId From(Guid value) => new(value);
    public static CapturedMessageId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();

    public int CompareTo(CapturedMessageId other) => Value.CompareTo(other.Value);

    public static bool operator <(CapturedMessageId left, CapturedMessageId right) => left.CompareTo(right) < 0;
    public static bool operator >(CapturedMessageId left, CapturedMessageId right) => left.CompareTo(right) > 0;
    public static bool operator <=(CapturedMessageId left, CapturedMessageId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(CapturedMessageId left, CapturedMessageId right) => left.CompareTo(right) >= 0;
}
