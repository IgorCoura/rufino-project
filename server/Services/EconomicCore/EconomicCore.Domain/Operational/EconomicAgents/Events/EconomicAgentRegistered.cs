namespace EconomicCore.Domain.Operational.EconomicAgents.Events;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed record EconomicAgentRegistered(
    Guid EventId,
    EconomicAgentId AgentId,
    TenantId TenantId,
    string Name,
    string ScopeName,
    string? TaxIdValue,
    string? TaxIdKindName,
    DateTime OccurredAt) : IDomainEvent;
