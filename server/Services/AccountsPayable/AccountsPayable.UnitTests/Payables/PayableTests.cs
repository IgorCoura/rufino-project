namespace AccountsPayable.UnitTests.Payables;

using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.Events;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.Payables.Mothers;

public class PayableTests
{
    private static readonly DateTime FIXED_NOW = PayableMother.DEFAULT_OCCURRED_AT;
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenInitializing
    {
        // Initialize com dados válidos cria Payable em Draft, Version=1, e popula todos os campos via When(PayableCreated).
        [Fact]
        public void Initialize_WithValidData_ShouldCreatePayableInDraftWithVersion1()
        {
            var payable = PayableMother.Draft();

            Assert.Equal(PayableStatus.Draft, payable.Status);
            Assert.Equal(1, payable.Version);
            Assert.Equal(PayableMother.DEFAULT_TENANT, payable.TenantId);
            Assert.Equal(PayableMother.DEFAULT_SUPPLIER, payable.SupplierId);
            Assert.Equal(1500m, payable.Amount.Amount);
            Assert.Equal(Currency.Brl, payable.Amount.Currency);
            Assert.Equal(PayableMother.DEFAULT_DUE_DATE, payable.DueDate.Value);
            Assert.Equal("Aluguel sede março", payable.Description.Value);
            Assert.Null(payable.ScheduledFor);
            Assert.Null(payable.PaidAt);
            Assert.Null(payable.PaymentProof);
        }

        // Initialize registra PayableCreated em Changes com payload completo (Currency e DueDate serializados).
        [Fact]
        public void Initialize_ShouldRecordPayableCreatedInChanges()
        {
            var payable = PayableMother.Draft();

            var change = Assert.Single(payable.Changes);
            var created = Assert.IsType<PayableCreated>(change);
            Assert.Equal(payable.Id, created.PayableId);
            Assert.Equal(1500m, created.AmountValue);
            Assert.Equal("BRL", created.AmountCurrency);
            Assert.Equal(PayableMother.DEFAULT_DUE_DATE, created.DueDate);
            Assert.Equal("Aluguel sede março", created.Description);
            Assert.Equal(FIXED_NOW, created.OccurredAt);
        }

        // Initialize com DueDate anterior à data de OccurredAt lança AP.PAY02 — Money e VOs já foram validados antes desta verificação contextual.
        [Fact]
        public void Initialize_WithDueDateInPast_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => PayableMother.Draft(
                dueDate: new DateOnly(2023, 1, 1),
                occurredAt: FIXED_NOW));

            Assert.Equal("AP.PAY02", ex.Id);
        }

        // Initialize com Money de valor negativo é rejeitado na construção do VO Money (AP.MON02), antes de chegar no Aggregate.
        [Fact]
        public void Initialize_WithNonPositiveAmount_ShouldThrowMoneyError()
        {
            var ex = Assert.Throws<DomainException>(() => PayableMother.Draft(amount: -10m));

            Assert.Equal("AP.MON02", ex.Id);
        }
    }

    public class WhenScheduling
    {
        // Schedule a partir de Draft muda status para Scheduled, define ScheduledFor e emite PayableScheduled.
        [Fact]
        public void Schedule_FromDraft_ShouldChangeStatusAndEmitEvent()
        {
            var payable = PayableMother.Draft();
            payable.PullChanges();

            payable.Schedule(PayableMother.DEFAULT_SCHEDULED_FOR, LATER);

            Assert.Equal(PayableStatus.Scheduled, payable.Status);
            Assert.Equal(PayableMother.DEFAULT_SCHEDULED_FOR, payable.ScheduledFor);
            var change = Assert.Single(payable.Changes);
            var scheduled = Assert.IsType<PayableScheduled>(change);
            Assert.Equal(payable.Id, scheduled.PayableId);
            Assert.Equal(PayableMother.DEFAULT_SCHEDULED_FOR, scheduled.ScheduledFor);
            Assert.Equal(LATER, scheduled.OccurredAt);
        }

        // Schedule a partir de Scheduled, Paid ou Cancelled é rejeitado (AP.PAY01).
        [Theory]
        [InlineData("Scheduled")]
        [InlineData("Paid")]
        [InlineData("Cancelled")]
        public void Schedule_FromNonDraft_ShouldThrowInvalidStatusTransition(string state)
        {
            var payable = state switch
            {
                "Scheduled" => PayableMother.Scheduled(),
                "Paid" => PayableMother.Paid(),
                "Cancelled" => PayableMother.Cancelled(),
                _ => throw new InvalidOperationException()
            };

            var ex = Assert.Throws<DomainException>(() => payable.Schedule(new DateOnly(2024, 3, 1), LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }
    }

    public class WhenMarkingAsPaid
    {
        // MarkAsPaidManually a partir de Scheduled muda status para Paid, define PaidAt e PaymentProof, e emite PayableMarkedAsPaid.
        [Fact]
        public void MarkAsPaidManually_FromScheduled_ShouldChangeStatusAndEmitEvent()
        {
            var payable = PayableMother.Scheduled();
            payable.PullChanges();
            var paidAt = FIXED_NOW.AddDays(30);

            payable.MarkAsPaidManually(PayableMother.DEFAULT_PROOF, paidAt, paidAt.AddMinutes(1));

            Assert.Equal(PayableStatus.Paid, payable.Status);
            Assert.Equal(paidAt, payable.PaidAt);
            Assert.Equal(PayableMother.DEFAULT_PROOF, payable.PaymentProof);
            var paid = Assert.IsType<PayableMarkedAsPaid>(payable.Changes.Single());
            Assert.Equal(payable.Id, paid.PayableId);
            Assert.Equal(paidAt, paid.PaidAt);
            Assert.Equal(PayableMother.DEFAULT_PROOF.Uri, paid.ProofUri);
            Assert.Equal("RECEIPT", paid.ProofType);
        }

        // MarkAsPaidManually a partir de Draft (sem agendar) é permitido — sprint plan: "cliente pode pagar antes de agendar".
        [Fact]
        public void MarkAsPaidManually_FromDraft_ShouldSucceed()
        {
            var payable = PayableMother.Draft();
            payable.PullChanges();
            var paidAt = FIXED_NOW.AddDays(1);

            payable.MarkAsPaidManually(PayableMother.DEFAULT_PROOF, paidAt, paidAt.AddMinutes(1));

            Assert.Equal(PayableStatus.Paid, payable.Status);
        }

        // MarkAsPaidManually em Payable Cancelled lança AP.PAY04 (CannotPayCancelled — erro dedicado, conforme critério de aceite da Sprint 2).
        [Fact]
        public void MarkAsPaidManually_FromCancelled_ShouldThrowCannotPayCancelled()
        {
            var payable = PayableMother.Cancelled();

            var ex = Assert.Throws<DomainException>(() => payable.MarkAsPaidManually(
                PayableMother.DEFAULT_PROOF, FIXED_NOW.AddDays(1), LATER));

            Assert.Equal("AP.PAY04", ex.Id);
        }

        // MarkAsPaidManually em Payable já Paid lança AP.PAY01 (transição inválida — Paid é terminal).
        [Fact]
        public void MarkAsPaidManually_FromPaid_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.Paid();

            var ex = Assert.Throws<DomainException>(() => payable.MarkAsPaidManually(
                PayableMother.DEFAULT_PROOF, FIXED_NOW.AddDays(2), LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }

        // MarkAsPaidManually com proof null lança ArgumentNullException (guarda básica antes da máquina de estados).
        [Fact]
        public void MarkAsPaidManually_WithNullProof_ShouldThrowArgumentNullException()
        {
            var payable = PayableMother.Scheduled();

            Assert.Throws<ArgumentNullException>(() =>
                payable.MarkAsPaidManually(null!, FIXED_NOW.AddDays(1), LATER));
        }
    }

    public class WhenCancelling
    {
        // Cancel a partir de Draft ou Scheduled muda status para Cancelled e emite PayableCancelled com a razão.
        [Theory]
        [InlineData("Draft")]
        [InlineData("Scheduled")]
        public void Cancel_FromActiveStates_ShouldChangeStatusAndEmitEvent(string state)
        {
            var payable = state == "Draft" ? PayableMother.Draft() : PayableMother.Scheduled();
            payable.PullChanges();

            payable.Cancel("Boleto duplicado", LATER);

            Assert.Equal(PayableStatus.Cancelled, payable.Status);
            var cancelled = Assert.IsType<PayableCancelled>(payable.Changes.Single());
            Assert.Equal("Boleto duplicado", cancelled.Reason);
        }

        // Cancel em Payable Paid ou Cancelled (terminais) lança AP.PAY01.
        [Theory]
        [InlineData("Paid")]
        [InlineData("Cancelled")]
        public void Cancel_FromTerminalStates_ShouldThrowDomainException(string state)
        {
            var payable = state == "Paid" ? PayableMother.Paid() : PayableMother.Cancelled();

            var ex = Assert.Throws<DomainException>(() => payable.Cancel("motivo qualquer", LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }

        // Cancel com motivo vazio/whitespace lança AP.PAY03 (Reason obrigatório).
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Cancel_WithEmptyReason_ShouldThrowDomainException(string reason)
        {
            var payable = PayableMother.Draft();

            var ex = Assert.Throws<DomainException>(() => payable.Cancel(reason, LATER));

            Assert.Equal("AP.PAY03", ex.Id);
        }
    }

    public class WhenRehydrating
    {
        // Given-When-Expect canônico do critério de aceite da Sprint 2:
        // Given: [PayableCreated, PayableScheduled].
        // When:  MarkAsPaidManually(...).
        // Expect: [PayableMarkedAsPaid] em Changes (history replayado não conta como mudança nova).
        [Fact]
        public void GivenCreatedAndScheduled_WhenMarkAsPaidManually_ShouldEmitOnlyMarkedAsPaidInChanges()
        {
            var payableId = PayableId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            var history = new IDomainEvent[]
            {
                new PayableCreated(
                    EventId: Guid.NewGuid(),
                    OccurredAt: FIXED_NOW,
                    PayableId: payableId,
                    TenantId: PayableMother.DEFAULT_TENANT,
                    SupplierId: PayableMother.DEFAULT_SUPPLIER,
                    AmountValue: 1_500m,
                    AmountCurrency: "BRL",
                    DueDate: PayableMother.DEFAULT_DUE_DATE,
                    Description: "Aluguel sede março"),
                new PayableScheduled(
                    EventId: Guid.NewGuid(),
                    OccurredAt: FIXED_NOW.AddMinutes(5),
                    PayableId: payableId,
                    ScheduledFor: PayableMother.DEFAULT_SCHEDULED_FOR),
            };
            var payable = Payable.Rehydrate(history);
            var paidAt = FIXED_NOW.AddDays(30);

            payable.MarkAsPaidManually(PayableMother.DEFAULT_PROOF, paidAt, paidAt.AddMinutes(1));

            var change = Assert.Single(payable.Changes);
            Assert.IsType<PayableMarkedAsPaid>(change);
            Assert.Equal(PayableStatus.Paid, payable.Status);
            Assert.Equal(3, payable.Version); // 2 do replay + 1 do Apply
        }

        // Rehydrate de stream completo (Created → Scheduled → MarkedAsPaid → Cancel attempt would fail) reconstrói o estado final como Paid sem registrar Changes.
        [Fact]
        public void Rehydrate_FromCompleteStream_ShouldRebuildFinalStateWithEmptyChanges()
        {
            var payableId = PayableId.From(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            var history = new IDomainEvent[]
            {
                new PayableCreated(
                    Guid.NewGuid(), FIXED_NOW, payableId,
                    PayableMother.DEFAULT_TENANT, PayableMother.DEFAULT_SUPPLIER,
                    1_500m, "BRL", PayableMother.DEFAULT_DUE_DATE, "Aluguel sede março"),
                new PayableScheduled(Guid.NewGuid(), FIXED_NOW.AddMinutes(5), payableId, PayableMother.DEFAULT_SCHEDULED_FOR),
                new PayableMarkedAsPaid(
                    Guid.NewGuid(), FIXED_NOW.AddDays(30), payableId,
                    PaidAt: FIXED_NOW.AddDays(30), ProofUri: PayableMother.DEFAULT_PROOF.Uri, ProofType: "RECEIPT"),
            };

            var payable = Payable.Rehydrate(history);

            Assert.Equal(payableId, payable.Id);
            Assert.Equal(PayableStatus.Paid, payable.Status);
            Assert.Equal(FIXED_NOW.AddDays(30), payable.PaidAt);
            Assert.Equal(PayableMother.DEFAULT_PROOF.Uri, payable.PaymentProof!.Uri);
            Assert.Equal(PaymentProofType.Receipt, payable.PaymentProof.Type);
            Assert.Equal(3, payable.Version);
            Assert.Empty(payable.Changes);
        }

        // PullChanges drena Changes mas não afeta Version nem estado — Repository chama isso após persistir.
        [Fact]
        public void PullChanges_AfterApply_ShouldEmptyChangesButKeepStateAndVersion()
        {
            var payable = PayableMother.Scheduled();

            var pulled = payable.PullChanges();

            Assert.Equal(2, pulled.Count); // PayableCreated + PayableScheduled
            Assert.Empty(payable.Changes);
            Assert.Equal(2, payable.Version);
            Assert.Equal(PayableStatus.Scheduled, payable.Status);
        }
    }
}
