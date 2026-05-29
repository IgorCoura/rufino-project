namespace EconomicCore.Infra.Persistence;

// Idempotency ledger: one row per processed x-requestid. PK on Id makes concurrent
// duplicates collide at the database level (unique violation), not just in app code.
public sealed class ClientRequest
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime Time { get; private set; }

    private ClientRequest() { }

    public ClientRequest(Guid id, string name, DateTime time)
    {
        Id = id;
        Name = name;
        Time = time;
    }
}
