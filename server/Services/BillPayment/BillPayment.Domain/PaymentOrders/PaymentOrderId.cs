namespace BillPayment.Domain.PaymentOrders;

using BillPayment.Domain.SeedWork;

/// <summary>Identidade da <see cref="PaymentOrder"/>.</summary>
/// <remarks>
/// Comparável pelo mesmo motivo dos outros cinco Ids paginados: o desempate do cursor de keyset
/// precisa compilar e ser traduzido pelo EF. A ordem que vale é a do <c>uuid</c> no Postgres —
/// não ordene em memória esperando a mesma sequência.
/// </remarks>
public readonly record struct PaymentOrderId(Guid Value) : IEntityId<PaymentOrderId>, IComparable<PaymentOrderId>
{
    public static PaymentOrderId New() => new(Guid.CreateVersion7());
    public static PaymentOrderId From(Guid value) => new(value);
    public static PaymentOrderId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();

    public int CompareTo(PaymentOrderId other) => Value.CompareTo(other.Value);

    public static bool operator <(PaymentOrderId left, PaymentOrderId right) => left.CompareTo(right) < 0;
    public static bool operator >(PaymentOrderId left, PaymentOrderId right) => left.CompareTo(right) > 0;
    public static bool operator <=(PaymentOrderId left, PaymentOrderId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(PaymentOrderId left, PaymentOrderId right) => left.CompareTo(right) >= 0;
}
