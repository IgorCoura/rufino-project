namespace BillPayment.Domain.CaptureSources;

using BillPayment.Domain.SeedWork;

/// <summary>Identidade da <see cref="CaptureSource"/>.</summary>
/// <remarks>
/// Comparável para servir de desempate na paginação por keyset — <c>CreatedAt</c> empata em massa
/// e sozinho esconde tudo além da primeira página (a história está em <c>CursorCodec</c>). A ordem
/// que vale é a do <c>uuid</c> no Postgres, onde <c>ORDER BY</c> e <c>WHERE</c> são ambos
/// avaliados; estes operadores existem para a expressão compilar e ser traduzida, e
/// <strong>não</strong> reproduzem a mesma sequência se a coleção for ordenada em memória.
/// </remarks>
public readonly record struct CaptureSourceId(Guid Value)
    : IEntityId<CaptureSourceId>, IComparable<CaptureSourceId>
{
    public static CaptureSourceId New() => new(Guid.CreateVersion7());
    public static CaptureSourceId From(Guid value) => new(value);
    public static CaptureSourceId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();

    public int CompareTo(CaptureSourceId other) => Value.CompareTo(other.Value);

    public static bool operator <(CaptureSourceId left, CaptureSourceId right) => left.CompareTo(right) < 0;
    public static bool operator >(CaptureSourceId left, CaptureSourceId right) => left.CompareTo(right) > 0;
    public static bool operator <=(CaptureSourceId left, CaptureSourceId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(CaptureSourceId left, CaptureSourceId right) => left.CompareTo(right) >= 0;
}
