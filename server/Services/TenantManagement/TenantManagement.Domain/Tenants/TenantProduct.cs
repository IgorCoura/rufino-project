namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;

/// <summary>
/// Um produto da plataforma habilitado para o tenant. Desabilitar não apaga a linha: o
/// histórico de quando o produto esteve ligado é o que explica cobrança e acesso passados.
/// </summary>
public sealed class TenantProduct : Entity<TenantProductId>
{
    public TenantId TenantId { get; private set; }
    public ProductCode Code { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    private TenantProduct() { }

    private TenantProduct(TenantProductId id) : base(id) { }

    internal static TenantProduct Activate(TenantId tenantId, ProductCode code, DateTime occurredAt)
        => new(TenantProductId.New())
        {
            TenantId = tenantId,
            Code = code,
            IsActive = true,
            ActivatedAt = occurredAt,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

    internal void Reactivate(DateTime occurredAt)
    {
        if (IsActive)
            return;

        IsActive = true;
        ActivatedAt = occurredAt;
        DeactivatedAt = null;
        UpdatedAt = occurredAt;
    }

    internal void Deactivate(DateTime occurredAt)
    {
        if (!IsActive)
            return;

        IsActive = false;
        DeactivatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }
}
