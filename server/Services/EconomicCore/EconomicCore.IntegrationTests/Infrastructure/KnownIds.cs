namespace EconomicCore.IntegrationTests.Infrastructure;

public static class KnownIds
{
    public static readonly Guid TenantA = new("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid TenantB = new("bbbbbbbb-0000-0000-0000-000000000002");
    public static readonly Guid Company = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Landlord = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CashRes = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid ServiceRes = new("44444444-4444-4444-4444-444444444444");
}

public static class KnownDates
{
    public static readonly DateTime CommitmentGen = new(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime Consumption = new(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc);
    public static readonly DateTime Payment = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
}
