namespace EconomicCore.Domain.Operational.EconomicResources.Events;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed record EconomicResourceRegistered(
    Guid EventId,
    EconomicResourceId ResourceId,
    TenantId TenantId,
    string Name,
    string KindName,
    Guid? TypeId,
    Guid? CustodianId,
    DateTime OccurredAt) : IDomainEvent;
