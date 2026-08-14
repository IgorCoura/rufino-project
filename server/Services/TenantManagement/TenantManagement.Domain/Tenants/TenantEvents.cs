namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;

/// <summary>Um tenant novo passou a existir na plataforma.</summary>
public sealed record TenantRegisteredDomainEvent(
    TenantId TenantId,
    string LegalName,
    string Kind,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}

/// <summary>
/// Alguém passou a ter acesso ao tenant. É o gatilho do provisionamento: o acesso só
/// existe de fato quando o provedor de identidade souber dele.
/// </summary>
public sealed record MembershipGrantedDomainEvent(
    TenantId TenantId,
    string Email,
    string Role,
    string TenantLegalName,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}

/// <summary>O acesso de alguém ao tenant foi cortado.</summary>
public sealed record MembershipRevokedDomainEvent(
    TenantId TenantId,
    string Email,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}

public sealed record TenantSuspendedDomainEvent(
    TenantId TenantId,
    string Reason,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}

public sealed record TenantReactivatedDomainEvent(
    TenantId TenantId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}

public sealed record ProductActivatedDomainEvent(
    TenantId TenantId,
    string Product,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}

public sealed record ProductDeactivatedDomainEvent(
    TenantId TenantId,
    string Product,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}
