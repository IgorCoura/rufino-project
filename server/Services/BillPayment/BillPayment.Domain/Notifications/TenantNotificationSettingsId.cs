namespace BillPayment.Domain.Notifications;

using BillPayment.Domain.SeedWork;

public readonly record struct TenantNotificationSettingsId(Guid Value)
    : IEntityId<TenantNotificationSettingsId>
{
    public static TenantNotificationSettingsId New() => new(Guid.CreateVersion7());

    public static TenantNotificationSettingsId From(Guid value) => new(value);

    public static TenantNotificationSettingsId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
