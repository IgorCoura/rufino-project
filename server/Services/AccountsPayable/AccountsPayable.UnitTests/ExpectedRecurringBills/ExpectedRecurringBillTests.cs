namespace AccountsPayable.UnitTests.ExpectedRecurringBills;

using AccountsPayable.Domain.ExpectedRecurringBills.Enumerations;
using AccountsPayable.Domain.ExpectedRecurringBills.Events;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.ExpectedRecurringBills.Mothers;

public class ExpectedRecurringBillTests
{
    private static readonly DateTime FIXED_NOW = ExpectedRecurringBillMother.DEFAULT_OCCURRED_AT;
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenCreating
    {
        // ForContract cria bill em Pending com todos os campos preenchidos e emite ExpectedRecurringBillCreated.
        [Fact]
        public void ForContract_WithValidInputs_ShouldStartPendingAndEmitEvent()
        {
            var bill = ExpectedRecurringBillMother.Pending();

            Assert.Equal(ExpectedRecurringBillStatus.Pending, bill.Status);
            Assert.Null(bill.MatchedPayableId);
            Assert.Equal(5_000m, bill.ExpectedAmount.Amount);
            var created = Assert.IsType<ExpectedRecurringBillCreated>(bill.PullDomainEvents().Single());
            Assert.Equal(bill.Id, created.BillId);
            Assert.Equal(ExpectedRecurringBillMother.DEFAULT_CONTRACT, created.ContractId);
        }
    }

    public class WhenMatching
    {
        // MatchToPayable em Pending vai pra MatchedToPayable, popula MatchedPayableId/MatchedAt e emite ExpectedRecurringBillMatched.
        [Fact]
        public void MatchToPayable_FromPending_ShouldChangeStatusAndEmitEvent()
        {
            var bill = ExpectedRecurringBillMother.Pending();
            bill.PullDomainEvents();
            var payableId = PayableId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            bill.MatchToPayable(payableId, LATER);

            Assert.Equal(ExpectedRecurringBillStatus.MatchedToPayable, bill.Status);
            Assert.Equal(payableId, bill.MatchedPayableId);
            Assert.Equal(LATER, bill.MatchedAt);
            var ev = Assert.IsType<ExpectedRecurringBillMatched>(bill.PullDomainEvents().Single());
            Assert.Equal(payableId, ev.MatchedPayableId);
        }

        // MatchToPayable em qualquer status não-Pending lança AP.ERB01.
        [Fact]
        public void MatchToPayable_OnNonPending_ShouldThrowDomainException()
        {
            var bill = ExpectedRecurringBillMother.Pending();
            bill.MarkMissed(LATER);

            var ex = Assert.Throws<DomainException>(() => bill.MatchToPayable(
                PayableId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), LATER.AddMinutes(1)));
            Assert.Equal("AP.ERB01", ex.Id);
        }
    }

    public class WhenMarkingMissed
    {
        // MarkMissed em Pending vai pra Missed e emite ExpectedRecurringBillMissed.
        [Fact]
        public void MarkMissed_FromPending_ShouldChangeStatusAndEmitEvent()
        {
            var bill = ExpectedRecurringBillMother.Pending();
            bill.PullDomainEvents();

            bill.MarkMissed(LATER);

            Assert.Equal(ExpectedRecurringBillStatus.Missed, bill.Status);
            Assert.Equal(LATER, bill.MissedAt);
            Assert.IsType<ExpectedRecurringBillMissed>(bill.PullDomainEvents().Single());
        }

        // MarkMissed em bill já MatchedToPayable lança AP.ERB01.
        [Fact]
        public void MarkMissed_OnMatched_ShouldThrowDomainException()
        {
            var bill = ExpectedRecurringBillMother.Pending();
            bill.MatchToPayable(PayableId.New(), LATER);

            var ex = Assert.Throws<DomainException>(() => bill.MarkMissed(LATER.AddMinutes(1)));
            Assert.Equal("AP.ERB01", ex.Id);
        }
    }

    public class WhenCancelling
    {
        // Cancel em Pending muda status, registra reason e emite ExpectedRecurringBillCancelled.
        [Fact]
        public void Cancel_FromPending_ShouldChangeStatusAndEmitEvent()
        {
            var bill = ExpectedRecurringBillMother.Pending();
            bill.PullDomainEvents();

            bill.Cancel("Contrato terminado", LATER);

            Assert.Equal(ExpectedRecurringBillStatus.Cancelled, bill.Status);
            Assert.Equal("Contrato terminado", bill.CancellationReason);
            Assert.Equal(LATER, bill.CancelledAt);
            Assert.IsType<ExpectedRecurringBillCancelled>(bill.PullDomainEvents().Single());
        }

        // Cancel com reason vazio lança AP.ERB02.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Cancel_WithEmptyReason_ShouldThrowDomainException(string reason)
        {
            var bill = ExpectedRecurringBillMother.Pending();
            var ex = Assert.Throws<DomainException>(() => bill.Cancel(reason, LATER));
            Assert.Equal("AP.ERB02", ex.Id);
        }

        // Cancel em bill já Matched lança AP.ERB01 (não dá pra cancelar conta já casada).
        [Fact]
        public void Cancel_OnMatched_ShouldThrowDomainException()
        {
            var bill = ExpectedRecurringBillMother.Pending();
            bill.MatchToPayable(PayableId.New(), LATER);

            var ex = Assert.Throws<DomainException>(() => bill.Cancel("motivo", LATER.AddMinutes(1)));
            Assert.Equal("AP.ERB01", ex.Id);
        }
    }
}
