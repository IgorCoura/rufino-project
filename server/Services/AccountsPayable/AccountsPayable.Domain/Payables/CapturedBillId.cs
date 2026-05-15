namespace AccountsPayable.Domain.Payables;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Reference to a <c>CapturedBill</c> aggregate that lives in the sibling <c>BillIngestion</c> BC.
/// Stored on <see cref="Payable"/> after <c>InitializeFromCapture</c> so the Application layer can
/// deduplicate redeliveries of <c>CapturedBillApproved</c> (uniqueness on <c>(TenantId, CapturedBillId)</c>
/// — enforced by Infra via DB unique index; the Domain only exposes the field for the query).
/// </summary>
public readonly record struct CapturedBillId(Guid Value) : IEntityId<CapturedBillId>
{
    public static CapturedBillId New() => new(Guid.NewGuid());
    public static CapturedBillId From(Guid value) => new(value);
    public static CapturedBillId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
