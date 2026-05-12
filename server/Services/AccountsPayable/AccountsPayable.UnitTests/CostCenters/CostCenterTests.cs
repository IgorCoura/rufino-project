namespace AccountsPayable.UnitTests.CostCenters;

using AccountsPayable.Domain.CostCenters.Events;
using AccountsPayable.Domain.CostCenters.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.CostCenters.Mothers;

public class CostCenterTests
{
    private static readonly DateTime FIXED_NOW = CostCenterMother.DEFAULT_OCCURRED_AT;
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenCreating
    {
        // Create monta CostCenter Active com Code/Name normalizados e emite CostCenterCreated.
        [Fact]
        public void Create_WithValidData_ShouldStartActiveAndEmitEvent()
        {
            var center = CostCenterMother.Active();

            Assert.True(center.IsActive);
            Assert.Equal("OBRA-SYRAH", center.Code.Value);
            Assert.Equal("Obra Syrah", center.Name.Value);
            var created = Assert.IsType<CostCenterCreated>(center.PullDomainEvents().Single());
            Assert.Equal(center.Id, created.CostCenterId);
            Assert.Equal("OBRA-SYRAH", created.Code);
        }
    }

    public class WhenRenaming
    {
        // Rename com novo Name muda o estado e emite CostCenterRenamed com oldName/newName.
        [Fact]
        public void Rename_WithDifferentName_ShouldUpdateAndEmitEvent()
        {
            var center = CostCenterMother.Active();
            center.PullDomainEvents();

            center.Rename(new CostCenterName("Obra Syrah - Fase 2"), LATER);

            var renamed = Assert.IsType<CostCenterRenamed>(center.PullDomainEvents().Single());
            Assert.Equal("Obra Syrah", renamed.OldName);
            Assert.Equal("Obra Syrah - Fase 2", renamed.NewName);
        }

        // Rename com mesmo nome atual é idempotente: sem evento.
        [Fact]
        public void Rename_WithSameName_ShouldBeNoOp()
        {
            var center = CostCenterMother.Active();
            center.PullDomainEvents();

            center.Rename(new CostCenterName("Obra Syrah"), LATER);

            Assert.Empty(center.PullDomainEvents());
        }
    }

    public class WhenDeactivating
    {
        // Deactivate em CostCenter Active muda para Inactive e emite CostCenterDeactivated.
        [Fact]
        public void Deactivate_WhenActive_ShouldChangeStatusAndEmitEvent()
        {
            var center = CostCenterMother.Active();
            center.PullDomainEvents();

            center.Deactivate(LATER);

            Assert.False(center.IsActive);
            Assert.IsType<CostCenterDeactivated>(center.PullDomainEvents().Single());
        }

        // Deactivate em CostCenter já Inactive lança AP.CCT02.
        [Fact]
        public void Deactivate_WhenAlreadyInactive_ShouldThrowDomainException()
        {
            var center = CostCenterMother.Inactive();

            var ex = Assert.Throws<DomainException>(() => center.Deactivate(LATER));

            Assert.Equal("AP.CCT02", ex.Id);
        }
    }

    public class WhenReactivating
    {
        // Reactivate em CostCenter Inactive muda para Active e emite CostCenterReactivated.
        [Fact]
        public void Reactivate_WhenInactive_ShouldChangeStatusAndEmitEvent()
        {
            var center = CostCenterMother.Inactive();
            center.PullDomainEvents();

            center.Reactivate(LATER);

            Assert.True(center.IsActive);
            Assert.IsType<CostCenterReactivated>(center.PullDomainEvents().Single());
        }

        // Reactivate em CostCenter já Active lança AP.CCT01.
        [Fact]
        public void Reactivate_WhenAlreadyActive_ShouldThrowDomainException()
        {
            var center = CostCenterMother.Active();

            var ex = Assert.Throws<DomainException>(() => center.Reactivate(LATER));

            Assert.Equal("AP.CCT01", ex.Id);
        }
    }
}
