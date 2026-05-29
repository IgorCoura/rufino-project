namespace EconomicCore.Infra.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    // When false, the background worker is not registered. Lets a single deployment own the relay
    // when the API scales horizontally (avoids redundant polling across replicas).
    public bool Enabled { get; set; } = true;

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    public int BatchSize { get; set; } = 20;

    public int MaxAttempts { get; set; } = 5;

    public int RetentionDays { get; set; } = 7;

    // Number of polling cycles between retention cleanups (~10min at the 5s default).
    public int CleanupEveryCycles { get; set; } = 120;
}
