namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;

/// <summary>
/// O acesso de uma pessoa a um tenant.
/// </summary>
/// <remarks>
/// A chave natural é o <strong>e-mail</strong>, não o <see cref="UserId"/>: no momento em que
/// o acesso é concedido a pessoa pode ainda não existir no provedor de identidade, e é o
/// provisionamento que devolve o identificador. Enquanto ele não volta, o vínculo já existe
/// e fica visível como pendente — em vez de o cadastro fingir que nada aconteceu.
/// </remarks>
public sealed class TenantMembership : Entity<TenantMembershipId>
{
    public const int MAX_LENGTH_EMAIL = 200;

    public TenantId TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public MembershipRole Role { get; private set; } = default!;

    /// <summary>Nulo até o provedor de identidade confirmar quem é a pessoa.</summary>
    public UserId? IdentityUserId { get; private set; }

    public ProvisioningStatus Provisioning { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private TenantMembership() { }

    private TenantMembership(TenantMembershipId id) : base(id) { }

    internal static TenantMembership Grant(TenantId tenantId, string email, MembershipRole role, DateTime occurredAt)
        => new(TenantMembershipId.New())
        {
            TenantId = tenantId,
            Email = NormalizeEmail(email),
            Role = role ?? throw TenantErrors.MembershipRoleRequired(),
            Provisioning = ProvisioningStatus.Pending,
            IsActive = true,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

    internal static string NormalizeEmail(string email)
    {
        var normalized = EmailSyntax.Normalize(email);
        if (normalized.Length == 0)
            throw TenantErrors.MembershipEmailRequired();
        if (normalized.Length > MAX_LENGTH_EMAIL || !EmailSyntax.IsValidAddress(normalized))
            throw TenantErrors.InvalidMembershipEmail(email);
        return normalized;
    }

    /// <summary>Reconceder acesso a quem já teve reaproveita o vínculo em vez de duplicá-lo.</summary>
    internal void Regrant(MembershipRole role, DateTime occurredAt)
    {
        Role = role ?? throw TenantErrors.MembershipRoleRequired();
        IsActive = true;
        RevokedAt = null;
        Provisioning = ProvisioningStatus.Pending;
        UpdatedAt = occurredAt;
    }

    internal void ChangeRole(MembershipRole role, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (Role.Equals(role))
            return;

        Role = role;
        UpdatedAt = occurredAt;
    }

    internal void Revoke(DateTime occurredAt)
    {
        IsActive = false;
        RevokedAt = occurredAt;
        Provisioning = ProvisioningStatus.Pending;
        UpdatedAt = occurredAt;
    }

    internal void ConfirmProvisioned(UserId? userId, DateTime occurredAt)
    {
        if (userId is { } id && !id.Equals(UserId.Empty))
            IdentityUserId = id;

        Provisioning = ProvisioningStatus.Done;
        UpdatedAt = occurredAt;
    }

    internal void MarkProvisioningFailed(DateTime occurredAt)
    {
        Provisioning = ProvisioningStatus.Failed;
        UpdatedAt = occurredAt;
    }

    /// <summary>Recoloca o vínculo na fila de provisionamento — o caminho do reprovisionamento.</summary>
    internal void MarkProvisioningPending(DateTime occurredAt)
    {
        Provisioning = ProvisioningStatus.Pending;
        UpdatedAt = occurredAt;
    }

    internal bool NeedsProvisioning() => !Provisioning.Equals(ProvisioningStatus.Done);
}
