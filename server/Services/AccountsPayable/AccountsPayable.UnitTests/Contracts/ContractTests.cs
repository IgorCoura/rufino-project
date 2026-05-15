namespace AccountsPayable.UnitTests.Contracts;

using AccountsPayable.Domain.Contracts.Enumerations;
using AccountsPayable.Domain.Contracts.Events;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.Contracts.Mothers;

public class ContractTests
{
    private static readonly DateTime FIXED_NOW = ContractMother.DEFAULT_OCCURRED_AT;
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenCreating
    {
        // Create monta contrato em Draft com todos os campos preenchidos e emite ContractCreated.
        [Fact]
        public void Create_WithValidInputs_ShouldStartDraftAndEmitEvent()
        {
            var c = ContractMother.Draft(monthlyAmount: 5_000m, paymentDay: 10);

            Assert.Equal(ContractStatus.Draft, c.Status);
            Assert.Equal(5_000m, c.MonthlyAmount.Amount);
            Assert.Equal(10, c.PaymentDay);
            Assert.False(c.AutoCreatePayable);
            var created = Assert.IsType<ContractCreated>(c.PullDomainEvents().Single());
            Assert.Equal(c.Id, created.ContractId);
            Assert.Equal(5_000m, created.MonthlyAmountValue);
            Assert.Equal("BRL", created.MonthlyAmountCurrency);
            Assert.Equal(10, created.PaymentDay);
        }

        // PaymentDay fora de [1..31] lança AP.CTR02.
        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        [InlineData(-1)]
        public void Create_WithPaymentDayOutOfRange_ShouldThrowDomainException(int day)
        {
            var ex = Assert.Throws<DomainException>(() => ContractMother.Draft(paymentDay: day));
            Assert.Equal("AP.CTR02", ex.Id);
        }

        // EndDate anterior à StartDate lança AP.CTR03.
        [Fact]
        public void Create_WithEndDateBeforeStartDate_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => ContractMother.Draft(
                startDate: new DateOnly(2024, 6, 1),
                endDate: new DateOnly(2024, 3, 1)));
            Assert.Equal("AP.CTR03", ex.Id);
        }
    }

    public class WhenActivating
    {
        // Activate em Draft muda para Active e emite ContractActivated.
        [Fact]
        public void Activate_FromDraft_ShouldChangeStatusAndEmitEvent()
        {
            var c = ContractMother.Draft();
            c.PullDomainEvents();

            c.Activate(LATER);

            Assert.Equal(ContractStatus.Active, c.Status);
            Assert.IsType<ContractActivated>(c.PullDomainEvents().Single());
        }

        // Activate em Active novamente lança AP.CTR01.
        [Fact]
        public void Activate_FromActive_ShouldThrowDomainException()
        {
            var c = ContractMother.Active();
            var ex = Assert.Throws<DomainException>(() => c.Activate(LATER));
            Assert.Equal("AP.CTR01", ex.Id);
        }
    }

    public class WhenSuspendingAndResuming
    {
        // Suspend em Active muda status, registra reason e emite ContractSuspended.
        [Fact]
        public void Suspend_FromActive_ShouldChangeStatusAndEmitEvent()
        {
            var c = ContractMother.Active();
            c.PullDomainEvents();

            c.Suspend("Cliente solicitou pausa", LATER);

            Assert.Equal(ContractStatus.Suspended, c.Status);
            Assert.Equal("Cliente solicitou pausa", c.SuspensionReason);
            Assert.IsType<ContractSuspended>(c.PullDomainEvents().Single());
        }

        // Suspend com motivo vazio lança AP.CTR05.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Suspend_WithEmptyReason_ShouldThrowDomainException(string reason)
        {
            var c = ContractMother.Active();
            var ex = Assert.Throws<DomainException>(() => c.Suspend(reason, LATER));
            Assert.Equal("AP.CTR05", ex.Id);
        }

        // Resume de Suspended volta para Active, limpa SuspensionReason e emite ContractResumed.
        [Fact]
        public void Resume_FromSuspended_ShouldFlipBackAndEmitEvent()
        {
            var c = ContractMother.Active();
            c.Suspend("Pausa temporária", LATER);
            c.PullDomainEvents();

            c.Resume(LATER.AddMinutes(1));

            Assert.Equal(ContractStatus.Active, c.Status);
            Assert.Null(c.SuspensionReason);
            Assert.IsType<ContractResumed>(c.PullDomainEvents().Single());
        }

        // Resume em Active (não suspenso) lança AP.CTR01.
        [Fact]
        public void Resume_FromActive_ShouldThrowDomainException()
        {
            var c = ContractMother.Active();
            var ex = Assert.Throws<DomainException>(() => c.Resume(LATER));
            Assert.Equal("AP.CTR01", ex.Id);
        }
    }

    public class WhenTerminating
    {
        // Terminate em Active vai pra Terminated, registra reason e emite ContractTerminated.
        [Fact]
        public void Terminate_FromActive_ShouldChangeStatusAndEmitEvent()
        {
            var c = ContractMother.Active();
            c.PullDomainEvents();

            c.Terminate("Fim do contrato", LATER);

            Assert.Equal(ContractStatus.Terminated, c.Status);
            Assert.Equal("Fim do contrato", c.TerminationReason);
            var ev = Assert.IsType<ContractTerminated>(c.PullDomainEvents().Single());
            Assert.Equal(c.Id, ev.ContractId);
            Assert.Equal("Fim do contrato", ev.Reason);
        }

        // Terminate em Terminated (terminal) lança AP.CTR01.
        [Fact]
        public void Terminate_FromTerminated_ShouldThrowDomainException()
        {
            var c = ContractMother.Active();
            c.Terminate("razão 1", LATER);

            var ex = Assert.Throws<DomainException>(() => c.Terminate("razão 2", LATER.AddMinutes(1)));
            Assert.Equal("AP.CTR01", ex.Id);
        }

        // Terminate com motivo vazio lança AP.CTR04.
        [Fact]
        public void Terminate_WithEmptyReason_ShouldThrowDomainException()
        {
            var c = ContractMother.Active();
            var ex = Assert.Throws<DomainException>(() => c.Terminate("  ", LATER));
            Assert.Equal("AP.CTR04", ex.Id);
        }
    }

    public class WhenUpdatingAmount
    {
        // UpdateAmount com valor novo muda MonthlyAmount e emite ContractAmountChanged com old/new/effective.
        [Fact]
        public void UpdateAmount_WithNewValue_ShouldChangeAndEmitEvent()
        {
            var c = ContractMother.Active(monthlyAmount: 5_000m);
            c.PullDomainEvents();
            var newAmount = new Money(6_000m, Currency.Brl);
            var effective = new DateOnly(2024, 7, 1);

            c.UpdateAmount(newAmount, effective, LATER);

            Assert.Equal(6_000m, c.MonthlyAmount.Amount);
            var changed = Assert.IsType<ContractAmountChanged>(c.PullDomainEvents().Single());
            Assert.Equal(5_000m, changed.OldAmountValue);
            Assert.Equal(6_000m, changed.NewAmountValue);
            Assert.Equal(effective, changed.EffectiveDate);
        }

        // UpdateAmount com mesmo valor lança AP.CTR06.
        [Fact]
        public void UpdateAmount_WithSameValue_ShouldThrowDomainException()
        {
            var c = ContractMother.Active(monthlyAmount: 5_000m);
            var ex = Assert.Throws<DomainException>(() => c.UpdateAmount(
                new Money(5_000m, Currency.Brl), new DateOnly(2024, 7, 1), LATER));
            Assert.Equal("AP.CTR06", ex.Id);
        }

        // UpdateAmount com moeda diferente lança AP.CTR07.
        [Fact]
        public void UpdateAmount_WithDifferentCurrency_ShouldThrowDomainException()
        {
            var c = ContractMother.Active(monthlyAmount: 5_000m);
            var ex = Assert.Throws<DomainException>(() => c.UpdateAmount(
                new Money(5_000m, Currency.Usd), new DateOnly(2024, 7, 1), LATER));
            Assert.Equal("AP.CTR07", ex.Id);
        }
    }
}
