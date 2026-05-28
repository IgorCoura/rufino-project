namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractTerminateTests
{
    // Terminate em contrato Draft é permitido (descartar contrato sem ativar) e emite ContractTerminated sem commitments.
    [Fact]
    public void Terminate_OnDraftContract_ShouldTransitionToTerminatedAndEmitEvent()
    {
        var contract = EconomicContractMother.New().Build();
        contract.ClearDomainEvents();

        contract.Terminate(EconomicContractMother.FixedOccurredAt);

        Assert.Same(ContractStatus.Terminated, contract.Status);
        Assert.Empty(contract.Commitments);

        var terminated = Assert.IsType<ContractTerminated>(Assert.Single(contract.PullDomainEvents()));
        Assert.Equal(contract.Id, terminated.ContractId);
        Assert.Equal(contract.TenantId, terminated.TenantId);
        Assert.Equal(EconomicContractMother.FixedOccurredAt, terminated.OccurredAt);
    }

    // Terminate em contrato Active move status para Terminated e emite ContractTerminated.
    [Fact]
    public void Terminate_OnActiveContract_ShouldTransitionToTerminatedAndEmitEvent()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.ClearDomainEvents();

        contract.Terminate(EconomicContractMother.FixedOccurredAt);

        Assert.Same(ContractStatus.Terminated, contract.Status);
        var terminated = Assert.IsType<ContractTerminated>(Assert.Single(contract.PullDomainEvents()));
        Assert.Equal(contract.Id, terminated.ContractId);
    }

    // Terminate em contrato Suspended também é permitido e emite ContractTerminated.
    [Fact]
    public void Terminate_OnSuspendedContract_ShouldTransitionToTerminatedAndEmitEvent()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.Suspend(EconomicContractMother.FixedOccurredAt);
        contract.ClearDomainEvents();

        contract.Terminate(EconomicContractMother.FixedOccurredAt);

        Assert.Same(ContractStatus.Terminated, contract.Status);
        Assert.IsType<ContractTerminated>(Assert.Single(contract.PullDomainEvents()));
    }

    // Terminate em contrato já Terminated lança ECC.CTR13 (transição inválida).
    [Fact]
    public void Terminate_OnAlreadyTerminatedContract_ShouldThrowECC_CTR13()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.Terminate(EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(
            () => contract.Terminate(EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR13", ex.Id);
    }
}
