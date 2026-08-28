namespace BillPayment.Infra.Persistence;

// Idempotency ledger: one row per processed x-requestid, scoped by tenant and command. The
// composite PK (tenant_id, id, name) makes concurrent duplicates collide at the database level
// (unique violation), not just in app code — and keeps one tenant's ids from ever shadowing
// another's.
public sealed class ClientRequest
{
    public Guid TenantId { get; private set; }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime Time { get; private set; }

    private ClientRequest() { }

    public ClientRequest(Guid tenantId, Guid id, string name, DateTime time)
    {
        TenantId = tenantId;
        Id = id;
        Name = name;
        Time = time;
    }
}
