namespace EconomicCore.UnitTests.Operational.EconomicAgents;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicAgents.Enumerations;
using EconomicCore.Domain.Operational.EconomicAgents.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.UnitTests.Operational.EconomicAgents.Mothers;

public class EconomicAgentTests
{
    private const string VALID_CPF = "52998224725";
    private static readonly TaxId ValidCpfTaxId = new(VALID_CPF, TaxIdKind.CPF);

    // Create válido com TaxId presente inicializa o estado e o timestamp dos campos herdados.
    [Fact]
    public void Create_WithValidInputs_ShouldInitializeStateCorrectly()
    {
        var agent = EconomicAgentMother.New()
            .WithTaxId(ValidCpfTaxId)
            .Build();

        Assert.Equal(EconomicAgentMother.FixedAgentId, agent.Id);
        Assert.Equal(EconomicAgentMother.FixedTenantId, agent.TenantId);
        Assert.Equal(EconomicAgentMother.DEFAULT_NAME, agent.Name);
        Assert.Same(AgentScope.Outside, agent.Scope);
        Assert.Equal(ValidCpfTaxId, agent.TaxId);
        Assert.Empty(agent.Roles);
        Assert.Equal(EconomicAgentMother.FixedOccurredAt, agent.CreatedAt);
        Assert.Equal(EconomicAgentMother.FixedOccurredAt, agent.UpdatedAt);
    }

    // Create válido com TaxId presente emite exatamente um EconomicAgentRegistered com payload completo (incluindo TaxId).
    [Fact]
    public void Create_WithTaxId_ShouldEmitEventCarryingTaxIdValueAndKind()
    {
        var agent = EconomicAgentMother.New()
            .WithTaxId(ValidCpfTaxId)
            .Build();

        var events = agent.PullDomainEvents();

        Assert.Single(events);
        var registered = Assert.IsType<EconomicAgentRegistered>(events[0]);
        Assert.Equal(agent.Id, registered.AgentId);
        Assert.Equal(agent.TenantId, registered.TenantId);
        Assert.Equal(agent.Name, registered.Name);
        Assert.Equal(AgentScope.Outside.Name, registered.ScopeName);
        Assert.Equal(VALID_CPF, registered.TaxIdValue);
        Assert.Equal("CPF", registered.TaxIdKindName);
        Assert.Equal(EconomicAgentMother.FixedOccurredAt, registered.OccurredAt);
        Assert.NotEqual(Guid.Empty, registered.EventId);
    }

    // Create válido sem TaxId emite evento com TaxIdValue/TaxIdKindName nulos.
    [Fact]
    public void Create_WithoutTaxId_ShouldEmitEventWithNullTaxIdFields()
    {
        var agent = EconomicAgentMother.New()
            .WithoutTaxId()
            .Build();

        var events = agent.PullDomainEvents();
        var registered = Assert.IsType<EconomicAgentRegistered>(events[0]);

        Assert.Null(registered.TaxIdValue);
        Assert.Null(registered.TaxIdKindName);
        Assert.Null(agent.TaxId);
    }

    // Nome vazio, whitespace ou null viola ECC.AGT01.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespaceName_ShouldThrowECC_AGT01(string? badName)
    {
        var mother = EconomicAgentMother.New().WithName(badName!);

        var ex = Assert.Throws<DomainException>(() => mother.Build());

        Assert.Equal("ECC.AGT01", ex.Id);
    }

    // Nome com tamanho acima de NAME_MAX_LENGTH (201+) viola ECC.AGT01.
    [Fact]
    public void Create_WithNameLongerThanMaxLength_ShouldThrowECC_AGT01()
    {
        var tooLong = new string('a', EconomicAgent.NAME_MAX_LENGTH + 1);
        var mother = EconomicAgentMother.New().WithName(tooLong);

        var ex = Assert.Throws<DomainException>(() => mother.Build());

        Assert.Equal("ECC.AGT01", ex.Id);
    }

    // Nome exatamente no limite (200 caracteres) é aceito (boundary inclusivo).
    [Fact]
    public void Create_WithNameAtMaxLength_ShouldSucceed()
    {
        var exactlyMax = new string('a', EconomicAgent.NAME_MAX_LENGTH);
        var agent = EconomicAgentMother.New().WithName(exactlyMax).Build();

        Assert.Equal(EconomicAgent.NAME_MAX_LENGTH, agent.Name.Length);
    }

    // AgentScope null viola ECC.AGT02 (Scope obrigatório por Axiom 3).
    [Fact]
    public void Create_WithNullScope_ShouldThrowECC_AGT02()
    {
        var mother = EconomicAgentMother.New().WithScope(null!);

        var ex = Assert.Throws<DomainException>(() => mother.Build());

        Assert.Equal("ECC.AGT02", ex.Id);
    }

    // Inside e Outside são preservados como Scope (não há default oculto).
    [Theory]
    [MemberData(nameof(AllAgentScopes))]
    public void Create_WithEitherScope_ShouldPersistTheGivenScope(AgentScope scope)
    {
        var agent = EconomicAgentMother.New().WithScope(scope).Build();

        Assert.Same(scope, agent.Scope);
    }

    public static IEnumerable<object[]> AllAgentScopes()
    {
        yield return new object[] { AgentScope.Inside };
        yield return new object[] { AgentScope.Outside };
    }

    // PullDomainEvents drena a lista interna — chamadas subsequentes retornam vazio.
    [Fact]
    public void PullDomainEvents_ShouldDrainEventsAndClearList()
    {
        var agent = EconomicAgentMother.New().Build();

        var firstDrain = agent.PullDomainEvents();
        var secondDrain = agent.PullDomainEvents();

        Assert.Single(firstDrain);
        Assert.Empty(secondDrain);
        Assert.Empty(agent.DomainEvents);
    }

    // Coleção Roles inicia vazia em Phase 1 (mutadores são da Fase 5).
    [Fact]
    public void Roles_AfterCreate_ShouldBeEmpty()
    {
        var agent = EconomicAgentMother.New().Build();

        Assert.Empty(agent.Roles);
    }
}
