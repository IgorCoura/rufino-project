namespace AccountsPayable.Domain.Payables;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Reference to a <c>PaymentOrder</c> aggregate that lives in the sibling <c>PaymentExecution</c> BC.
/// Stored on <see cref="Payable"/> after <c>RequestPayment</c> so confirmation/failure events coming
/// back from <c>PaymentExecution</c> can be matched and processed idempotently.
/// </summary>
public readonly record struct PaymentOrderId(Guid Value) : IEntityId<PaymentOrderId>
{
    public static PaymentOrderId New() => new(Guid.NewGuid());
    public static PaymentOrderId From(Guid value) => new(value);
    public static PaymentOrderId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
