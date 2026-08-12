namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.SeedWork;

/// <summary>Identidade da <see cref="BillExpectation"/>.</summary>
/// <remarks>
/// Comparável porque a lista de expectativas é paginada por keyset, e <c>CreatedAt</c> empata
/// quando o aprendizado cria várias no mesmo ciclo do job — a história está em <c>CursorCodec</c>.
/// </remarks>
public readonly record struct BillExpectationId(Guid Value)
    : IEntityId<BillExpectationId>, IComparable<BillExpectationId>
{
    public static BillExpectationId New() => new(Guid.CreateVersion7());
    public static BillExpectationId From(Guid value) => new(value);
    public static BillExpectationId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();

    public int CompareTo(BillExpectationId other) => Value.CompareTo(other.Value);

    public static bool operator <(BillExpectationId left, BillExpectationId right) => left.CompareTo(right) < 0;
    public static bool operator >(BillExpectationId left, BillExpectationId right) => left.CompareTo(right) > 0;
    public static bool operator <=(BillExpectationId left, BillExpectationId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(BillExpectationId left, BillExpectationId right) => left.CompareTo(right) >= 0;
}
