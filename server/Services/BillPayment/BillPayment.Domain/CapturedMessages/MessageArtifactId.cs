namespace BillPayment.Domain.CapturedMessages;

using BillPayment.Domain.SeedWork;

public readonly record struct MessageArtifactId(Guid Value) : IEntityId<MessageArtifactId>
{
    public static MessageArtifactId New() => new(Guid.CreateVersion7());
    public static MessageArtifactId From(Guid value) => new(value);
    public static MessageArtifactId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
