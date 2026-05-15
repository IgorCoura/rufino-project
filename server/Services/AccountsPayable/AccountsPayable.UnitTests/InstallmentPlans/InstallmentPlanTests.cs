namespace AccountsPayable.UnitTests.InstallmentPlans;

using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.InstallmentPlans.Enumerations;
using AccountsPayable.Domain.InstallmentPlans.Events;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.InstallmentPlans.Mothers;

public class InstallmentPlanTests
{
    private static readonly DateTime FIXED_NOW = InstallmentPlanMother.DEFAULT_OCCURRED_AT;
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenCreating
    {
        // Create monta InstallmentPlan Active, persiste campos e emite InstallmentPlanCreated com payload completo.
        [Fact]
        public void Create_WithValidData_ShouldStartActiveAndEmitEvent()
        {
            var plan = InstallmentPlanMother.Active();

            Assert.Equal(InstallmentPlanStatus.Active, plan.Status);
            Assert.Equal(12, plan.InstallmentCount);
            Assert.Equal(12_000m, plan.TotalAmount.Amount);
            Assert.Equal(Currency.Brl, plan.TotalAmount.Currency);
            Assert.Equal(InstallmentFrequency.Monthly, plan.Frequency);
            Assert.Empty(plan.PayableIds);
            var created = Assert.IsType<InstallmentPlanCreated>(plan.PullDomainEvents().Single());
            Assert.Equal(plan.Id, created.InstallmentPlanId);
            Assert.Equal(12, created.InstallmentCount);
            Assert.Equal("MONTHLY", created.Frequency);
            Assert.Equal("Aluguel anual", created.Description);
        }

        // Critério de aceite Sprint 8: não dá pra criar plano com InstallmentCount = 1 (não é parcelamento).
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-3)]
        public void Create_WithInstallmentCountBelowMinimum_ShouldThrowDomainException(int count)
        {
            var ex = Assert.Throws<DomainException>(() => InstallmentPlanMother.Active(installmentCount: count));

            Assert.Equal("AP.IPL01", ex.Id);
        }
    }

    public class WhenRegisteringPayable
    {
        // RegisterPayable em plano Active adiciona o vínculo, emite PayableLinkedToInstallmentPlan e popula PayableIds.
        [Fact]
        public void RegisterPayable_OnActivePlan_ShouldLinkAndEmitEvent()
        {
            var plan = InstallmentPlanMother.Active();
            plan.PullDomainEvents();
            var payableId = PayableId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            plan.RegisterPayable(payableId, installmentNumber: 1, LATER);

            Assert.Single(plan.PayableIds);
            Assert.Equal(payableId, plan.PayableIds[0]);
            var linked = Assert.IsType<PayableLinkedToInstallmentPlan>(plan.PullDomainEvents().Single());
            Assert.Equal(payableId, linked.PayableId);
            Assert.Equal(1, linked.InstallmentNumber);
            Assert.Equal(plan.Id, linked.InstallmentPlanId);
        }

        // RegisterPayable com installmentNumber fora do intervalo [1..InstallmentCount] lança AP.IPL02.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(13)]
        [InlineData(100)]
        public void RegisterPayable_WithOutOfRangeNumber_ShouldThrowDomainException(int installmentNumber)
        {
            var plan = InstallmentPlanMother.Active(installmentCount: 12);

            var ex = Assert.Throws<DomainException>(() => plan.RegisterPayable(
                PayableId.New(), installmentNumber, LATER));

            Assert.Equal("AP.IPL02", ex.Id);
        }

        // RegisterPayable de uma parcela já registrada lança AP.IPL03 (proteção contra duplicidade).
        [Fact]
        public void RegisterPayable_OnAlreadyRegisteredNumber_ShouldThrowDomainException()
        {
            var plan = InstallmentPlanMother.Active();
            plan.RegisterPayable(PayableId.New(), installmentNumber: 5, LATER);

            var ex = Assert.Throws<DomainException>(() => plan.RegisterPayable(
                PayableId.New(), installmentNumber: 5, LATER.AddMinutes(1)));

            Assert.Equal("AP.IPL03", ex.Id);
        }

        // RegisterPayable em plano cancelado lança AP.IPL06 (não aceita novos vínculos após cancelamento).
        [Fact]
        public void RegisterPayable_OnCancelledPlan_ShouldThrowDomainException()
        {
            var plan = InstallmentPlanMother.Active();
            plan.Cancel("Plano cancelado pelo cliente", LATER);

            var ex = Assert.Throws<DomainException>(() => plan.RegisterPayable(
                PayableId.New(), installmentNumber: 1, LATER.AddMinutes(1)));

            Assert.Equal("AP.IPL06", ex.Id);
        }
    }

    public class WhenCancelling
    {
        // Cancel em plano Active muda status, registra motivo e emite InstallmentPlanCancelled com a lista de PayableIds vinculados.
        [Fact]
        public void Cancel_OnActivePlan_ShouldChangeStatusAndEmitEventWithLinkedPayables()
        {
            var plan = InstallmentPlanMother.Active();
            var p1 = PayableId.From(new Guid("11111111-1111-1111-1111-aaaaaaaaaaaa"));
            var p2 = PayableId.From(new Guid("22222222-2222-2222-2222-aaaaaaaaaaaa"));
            plan.RegisterPayable(p1, 1, LATER);
            plan.RegisterPayable(p2, 2, LATER);
            plan.PullDomainEvents();

            plan.Cancel("Cliente desistiu do contrato", LATER.AddMinutes(2));

            Assert.Equal(InstallmentPlanStatus.Cancelled, plan.Status);
            Assert.Equal("Cliente desistiu do contrato", plan.CancellationReason);
            var cancelled = Assert.IsType<InstallmentPlanCancelled>(plan.PullDomainEvents().Single());
            Assert.Equal("Cliente desistiu do contrato", cancelled.Reason);
            // Critério Sprint 8: o evento carrega os PayableIds vinculados para o handler aplicar o cancel em cascata.
            Assert.Equal(2, cancelled.LinkedPayableIds.Count);
            Assert.Contains(p1, cancelled.LinkedPayableIds);
            Assert.Contains(p2, cancelled.LinkedPayableIds);
        }

        // Cancel num plano já cancelado lança AP.IPL04.
        [Fact]
        public void Cancel_OnAlreadyCancelledPlan_ShouldThrowDomainException()
        {
            var plan = InstallmentPlanMother.Active();
            plan.Cancel("motivo 1", LATER);

            var ex = Assert.Throws<DomainException>(() => plan.Cancel("motivo 2", LATER.AddMinutes(1)));

            Assert.Equal("AP.IPL04", ex.Id);
        }

        // Cancel com motivo vazio/whitespace lança AP.IPL05.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Cancel_WithEmptyReason_ShouldThrowDomainException(string reason)
        {
            var plan = InstallmentPlanMother.Active();

            var ex = Assert.Throws<DomainException>(() => plan.Cancel(reason, LATER));

            Assert.Equal("AP.IPL05", ex.Id);
        }
    }
}
