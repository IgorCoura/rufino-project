namespace BillPayment.Domain.Payees;

using BillPayment.Domain.SeedWork;

/// <summary>Identidade do <see cref="Payee"/>.</summary>
/// <remarks>
/// Comparável para servir de desempate na paginação por keyset — <c>CreatedAt</c> empata em massa
/// e sozinho esconde tudo além da primeira página (a história está em <c>CursorCodec</c>). A ordem
/// que vale é a do <c>uuid</c> no Postgres, onde <c>ORDER BY</c> e <c>WHERE</c> são ambos
/// avaliados; estes operadores existem para a expressão compilar e ser traduzida, e
/// <strong>não</strong> reproduzem a mesma sequência se a coleção for ordenada em memória.
/// </remarks>
public readonly record struct PayeeId(Guid Value) : IEntityId<PayeeId>, IComparable<PayeeId>
{
    public static PayeeId New() => new(Guid.CreateVersion7());
    public static PayeeId From(Guid value) => new(value);
    public static PayeeId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();

    public int CompareTo(PayeeId other) => Value.CompareTo(other.Value);

    public static bool operator <(PayeeId left, PayeeId right) => left.CompareTo(right) < 0;
    public static bool operator >(PayeeId left, PayeeId right) => left.CompareTo(right) > 0;
    public static bool operator <=(PayeeId left, PayeeId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(PayeeId left, PayeeId right) => left.CompareTo(right) >= 0;
}
