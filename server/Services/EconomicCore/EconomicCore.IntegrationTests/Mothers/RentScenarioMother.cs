namespace EconomicCore.IntegrationTests.Mothers;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicAgents.Enumerations;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Operational.EconomicResources.Enumerations;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.IntegrationTests.Infrastructure;

public static class RentScenarioMother
{
    private static readonly DateTime SeededAt = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public static EconomicAgent Company() => EconomicAgent.Create(
        EconomicAgentId.From(KnownIds.Company),
        TenantId.From(KnownIds.TenantA),
        "Minha PME Ltda",
        AgentScope.Inside,
        taxId: null,
        occurredAt: SeededAt);

    public static EconomicAgent Landlord() => EconomicAgent.Create(
        EconomicAgentId.From(KnownIds.Landlord),
        TenantId.From(KnownIds.TenantA),
        "Imobiliária Silva",
        AgentScope.Outside,
        taxId: null,
        occurredAt: SeededAt);

    public static EconomicResource Cash() => EconomicResource.Create(
        EconomicResourceId.From(KnownIds.CashRes),
        TenantId.From(KnownIds.TenantA),
        "Conta Corrente",
        ResourceKind.Cash,
        typeId: null,
        custodianId: null,
        occurredAt: SeededAt);

    public static EconomicResource RentService() => EconomicResource.Create(
        EconomicResourceId.From(KnownIds.ServiceRes),
        TenantId.From(KnownIds.TenantA),
        "Uso do imóvel",
        ResourceKind.Service,
        typeId: null,
        custodianId: null,
        occurredAt: SeededAt);

    public static EconomicAgent CompanyForTenantB() => EconomicAgent.Create(
        EconomicAgentId.From(new Guid("11111111-2222-2222-2222-111111111111")),
        TenantId.From(KnownIds.TenantB),
        "Empresa Tenant B",
        AgentScope.Inside,
        taxId: null,
        occurredAt: SeededAt);

    public static EconomicAgent LandlordForTenantB() => EconomicAgent.Create(
        EconomicAgentId.From(new Guid("22222222-3333-3333-3333-222222222222")),
        TenantId.From(KnownIds.TenantB),
        "Locador Tenant B",
        AgentScope.Outside,
        taxId: null,
        occurredAt: SeededAt);

    public static EconomicResource CashForTenantB() => EconomicResource.Create(
        EconomicResourceId.From(new Guid("33333333-4444-4444-4444-333333333333")),
        TenantId.From(KnownIds.TenantB),
        "Conta Corrente B",
        ResourceKind.Cash,
        typeId: null,
        custodianId: null,
        occurredAt: SeededAt);

    public static EconomicResource ServiceForTenantB() => EconomicResource.Create(
        EconomicResourceId.From(new Guid("44444444-5555-5555-5555-444444444444")),
        TenantId.From(KnownIds.TenantB),
        "Uso do imóvel B",
        ResourceKind.Service,
        typeId: null,
        custodianId: null,
        occurredAt: SeededAt);
}
