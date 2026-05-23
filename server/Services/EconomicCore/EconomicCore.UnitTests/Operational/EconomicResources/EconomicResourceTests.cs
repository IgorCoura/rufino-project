namespace EconomicCore.UnitTests.Operational.EconomicResources;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Operational.EconomicResources.Enumerations;
using EconomicCore.Domain.Operational.EconomicResources.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.UnitTests.Operational.EconomicResources.Mothers;

public class EconomicResourceTests
{
    // Create válido (caixa sem custodian/typeId) inicializa o estado e emite 1 EconomicResourceRegistered.
    [Fact]
    public void Create_WithMinimalValidInputs_ShouldInitializeStateAndEmitEvent()
    {
        var resource = EconomicResourceMother.New().Build();

        Assert.Equal(EconomicResourceMother.FixedResourceId, resource.Id);
        Assert.Equal(EconomicResourceMother.FixedTenantId, resource.TenantId);
        Assert.Equal(EconomicResourceMother.DEFAULT_NAME, resource.Name);
        Assert.Same(ResourceKind.Cash, resource.Kind);
        Assert.Null(resource.TypeId);
        Assert.Null(resource.CustodianId);
        Assert.Equal(EconomicResourceMother.FixedOccurredAt, resource.CreatedAt);
        Assert.Equal(EconomicResourceMother.FixedOccurredAt, resource.UpdatedAt);

        var events = resource.PullDomainEvents();
        var registered = Assert.IsType<EconomicResourceRegistered>(Assert.Single(events));
        Assert.Equal(resource.Id, registered.ResourceId);
        Assert.Equal(resource.TenantId, registered.TenantId);
        Assert.Equal(resource.Name, registered.Name);
        Assert.Equal(ResourceKind.Cash.Name, registered.KindName);
        Assert.Null(registered.TypeId);
        Assert.Null(registered.CustodianId);
        Assert.Equal(EconomicResourceMother.FixedOccurredAt, registered.OccurredAt);
    }

    // Create com TypeId e CustodianId propaga ambos os Guids no payload do evento.
    [Fact]
    public void Create_WithTypeIdAndCustodian_ShouldEmitEventCarryingBothGuids()
    {
        var typeId = EconomicResourceTypeId.From(new Guid("44444444-4444-7444-8444-444444444444"));
        var custodianId = EconomicAgentId.From(new Guid("55555555-5555-7555-8555-555555555555"));

        var resource = EconomicResourceMother.New()
            .WithTypeId(typeId)
            .WithCustodian(custodianId)
            .Build();

        Assert.Equal(typeId, resource.TypeId);
        Assert.Equal(custodianId, resource.CustodianId);

        var registered = (EconomicResourceRegistered)resource.PullDomainEvents()[0];
        Assert.Equal(typeId.Value, registered.TypeId);
        Assert.Equal(custodianId.Value, registered.CustodianId);
    }

    // Name vazio, whitespace ou null viola ECC.RES01.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespaceName_ShouldThrowECC_RES01(string? badName)
    {
        var mother = EconomicResourceMother.New().WithName(badName!);

        var ex = Assert.Throws<DomainException>(() => mother.Build());

        Assert.Equal("ECC.RES01", ex.Id);
    }

    // Name acima de NAME_MAX_LENGTH (201+) viola ECC.RES01.
    [Fact]
    public void Create_WithNameLongerThanMaxLength_ShouldThrowECC_RES01()
    {
        var tooLong = new string('a', EconomicResource.NAME_MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => EconomicResourceMother.New().WithName(tooLong).Build());

        Assert.Equal("ECC.RES01", ex.Id);
    }

    // Name no boundary (NAME_MAX_LENGTH) é aceito.
    [Fact]
    public void Create_WithNameAtMaxLength_ShouldSucceed()
    {
        var exactlyMax = new string('a', EconomicResource.NAME_MAX_LENGTH);

        var resource = EconomicResourceMother.New().WithName(exactlyMax).Build();

        Assert.Equal(EconomicResource.NAME_MAX_LENGTH, resource.Name.Length);
    }

    // Kind null viola ECC.RES02.
    [Fact]
    public void Create_WithNullKind_ShouldThrowECC_RES02()
    {
        var mother = EconomicResourceMother.New().WithKind(null!);

        var ex = Assert.Throws<DomainException>(() => mother.Build());

        Assert.Equal("ECC.RES02", ex.Id);
    }

    // Cada ResourceKind (Cash/Service/LaborService/FiscalObligation) é preservado no AR e propagado no evento.
    [Theory]
    [MemberData(nameof(AllResourceKinds))]
    public void Create_WithAnyResourceKind_ShouldPersistKindAndPropagateInEvent(ResourceKind kind)
    {
        var resource = EconomicResourceMother.New().WithKind(kind).Build();

        Assert.Same(kind, resource.Kind);
        var registered = (EconomicResourceRegistered)resource.PullDomainEvents()[0];
        Assert.Equal(kind.Name, registered.KindName);
    }

    public static IEnumerable<object[]> AllResourceKinds()
    {
        yield return new object[] { ResourceKind.Cash };
        yield return new object[] { ResourceKind.Service };
        yield return new object[] { ResourceKind.LaborService };
        yield return new object[] { ResourceKind.FiscalObligation };
    }

    // PullDomainEvents drena a lista; chamadas subsequentes retornam vazio.
    [Fact]
    public void PullDomainEvents_ShouldDrainEventsAndClearList()
    {
        var resource = EconomicResourceMother.New().Build();

        var firstDrain = resource.PullDomainEvents();
        var secondDrain = resource.PullDomainEvents();

        Assert.Single(firstDrain);
        Assert.Empty(secondDrain);
    }
}
