namespace BillPayment.Domain.Retention;

using BillPayment.Domain.SeedWork;

public readonly record struct CaptureRetentionPolicyId(Guid Value) : IEntityId<CaptureRetentionPolicyId>
{
    public static CaptureRetentionPolicyId New() => new(Guid.CreateVersion7());
    public static CaptureRetentionPolicyId From(Guid value) => new(value);
    public static CaptureRetentionPolicyId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
