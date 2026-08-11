namespace BillPayment.Domain.CaptureSources;

using BillPayment.Domain.SeedWork;

/// <summary>Identidade de uma <see cref="MonitoredFolder"/>.</summary>
public readonly record struct MonitoredFolderId(Guid Value) : IEntityId<MonitoredFolderId>
{
    public static MonitoredFolderId New() => new(Guid.CreateVersion7());
    public static MonitoredFolderId From(Guid value) => new(value);
    public static MonitoredFolderId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
