namespace AccountsPayable.UnitTests.Payables;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
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

    public class WhenClassifying
    {
        // Classify em Draft popula AccountId/CostCenterId/ClassifiedBy/ClassifiedAt e emite PayableClassified.
        [Fact]
        public void Classify_FromDraft_ShouldPopulateStateAndEmitEvent()
        {
            var payable = PayableMother.Draft();
            payable.PullChanges();

            payable.Classify(
                PayableMother.DEFAULT_ACCOUNT,
                PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_USER,
                LATER);

            Assert.Equal(PayableMother.DEFAULT_ACCOUNT, payable.AccountId);
            Assert.Equal(PayableMother.DEFAULT_COST_CENTER, payable.CostCenterId);
            Assert.Equal(PayableMother.DEFAULT_USER, payable.ClassifiedBy);
            Assert.Equal(LATER, payable.ClassifiedAt);
            var classified = Assert.IsType<PayableClassified>(payable.Changes.Single());
            Assert.Equal(payable.Id, classified.PayableId);
            Assert.Equal(PayableMother.DEFAULT_ACCOUNT, classified.AccountId);
        }

        // Reclassify em Draft já classificado é permitido — emite novo PayableClassified (A+ES preserva histórico).
        [Fact]
        public void Classify_OnAlreadyClassifiedDraft_ShouldEmitNewEvent()
        {
            var payable = PayableMother.Classified();
            payable.PullChanges();
            var newAccount = AccountId.From(new Guid("88888888-8888-8888-8888-888888888888"));

            payable.Classify(newAccount, PayableMother.DEFAULT_COST_CENTER, PayableMother.DEFAULT_USER, LATER);

            Assert.Equal(newAccount, payable.AccountId);
            var reclassified = Assert.IsType<PayableClassified>(payable.Changes.Single());
            Assert.Equal(newAccount, reclassified.AccountId);
        }

        // Reclassify em Scheduled é permitido (Status não-terminal).
        [Fact]
        public void Classify_OnScheduled_ShouldSucceed()
        {
            var payable = PayableMother.Scheduled();
            payable.PullChanges();
            var newCostCenter = CostCenterId.From(new Guid("77777777-7777-7777-7777-777777777777"));

            payable.Classify(PayableMother.DEFAULT_ACCOUNT, newCostCenter, PayableMother.DEFAULT_USER, LATER);

            Assert.Equal(newCostCenter, payable.CostCenterId);
            Assert.Equal(PayableStatus.Scheduled, payable.Status); // status permanece
        }

        // Classify em Payable Paid (terminal) lança AP.PAY06.
        [Fact]
        public void Classify_OnPaid_ShouldThrowDomainException()
        {
            var payable = PayableMother.Paid();

            var ex = Assert.Throws<DomainException>(() => payable.Classify(
                PayableMother.DEFAULT_ACCOUNT, PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_USER, LATER));

            Assert.Equal("AP.PAY06", ex.Id);
        }

        // Classify em Payable Cancelled (terminal) lança AP.PAY06.
        [Fact]
        public void Classify_OnCancelled_ShouldThrowDomainException()
        {
            var payable = PayableMother.Cancelled();

            var ex = Assert.Throws<DomainException>(() => payable.Classify(
                PayableMother.DEFAULT_ACCOUNT, PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_USER, LATER));

            Assert.Equal("AP.PAY06", ex.Id);
        }
    }

    public class WhenRequestingApproval
    {
        // RequestApproval em Draft classificado muda status para AwaitingApproval e emite PayableApprovalRequested.
        [Fact]
        public void RequestApproval_FromClassifiedDraft_ShouldChangeStatusAndEmitEvent()
        {
            var payable = PayableMother.Classified();
            payable.PullChanges();

            payable.RequestApproval(LATER);

            Assert.Equal(PayableStatus.AwaitingApproval, payable.Status);
            var requested = Assert.IsType<PayableApprovalRequested>(payable.Changes.Single());
            Assert.Equal(payable.Id, requested.PayableId);
            Assert.Equal(LATER, requested.OccurredAt);
        }

        // RequestApproval em Draft sem classificação lança AP.PAY08.
        [Fact]
        public void RequestApproval_FromUnclassifiedDraft_ShouldThrowDomainException()
        {
            var payable = PayableMother.Draft();

            var ex = Assert.Throws<DomainException>(() => payable.RequestApproval(LATER));

            Assert.Equal("AP.PAY08", ex.Id);
        }

        // RequestApproval em Scheduled lança AP.PAY01 (Scheduled não volta para AwaitingApproval).
        [Fact]
        public void RequestApproval_FromScheduled_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.Scheduled();

            var ex = Assert.Throws<DomainException>(() => payable.RequestApproval(LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }

        // RequestApproval duas vezes seguidas falha — AwaitingApproval não solicita aprovação novamente.
        [Fact]
        public void RequestApproval_FromAwaitingApproval_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.AwaitingApproval();

            var ex = Assert.Throws<DomainException>(() => payable.RequestApproval(LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }
    }

    public class WhenApproving
    {
        // Approve em AwaitingApproval muda status para Approved e registra ApprovedBy/ApprovedAt.
        [Fact]
        public void Approve_FromAwaitingApproval_ShouldChangeStatusAndRecordApprover()
        {
            var payable = PayableMother.AwaitingApproval();
            payable.PullChanges();

            payable.Approve(PayableMother.DEFAULT_USER, LATER);

            Assert.Equal(PayableStatus.Approved, payable.Status);
            Assert.Equal(PayableMother.DEFAULT_USER, payable.ApprovedBy);
            Assert.Equal(LATER, payable.ApprovedAt);
            var approved = Assert.IsType<PayableApproved>(payable.Changes.Single());
            Assert.Equal(PayableMother.DEFAULT_USER, approved.ApprovedBy);
        }

        // Approve em Draft lança AP.PAY01 — pular AwaitingApproval não é permitido.
        [Fact]
        public void Approve_FromDraft_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.Classified();

            var ex = Assert.Throws<DomainException>(() => payable.Approve(PayableMother.DEFAULT_USER, LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }

        // Approve em status já Approved lança AP.PAY01 (não reaprova).
        [Fact]
        public void Approve_WhenAlreadyApproved_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.Approved();

            var ex = Assert.Throws<DomainException>(() => payable.Approve(PayableMother.DEFAULT_USER, LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }
    }

    public class WhenRejecting
    {
        // Reject em AwaitingApproval muda status para Rejected e registra rejector + reason.
        [Fact]
        public void Reject_FromAwaitingApproval_ShouldChangeStatusAndRecordRejectorAndReason()
        {
            var payable = PayableMother.AwaitingApproval();
            payable.PullChanges();

            payable.Reject(PayableMother.DEFAULT_USER, "Valor inconsistente", LATER);

            Assert.Equal(PayableStatus.Rejected, payable.Status);
            Assert.Equal(PayableMother.DEFAULT_USER, payable.RejectedBy);
            Assert.Equal(LATER, payable.RejectedAt);
            Assert.Equal("Valor inconsistente", payable.RejectionReason);
            var rejected = Assert.IsType<PayableRejected>(payable.Changes.Single());
            Assert.Equal("Valor inconsistente", rejected.Reason);
        }

        // Reject sem motivo (vazio/whitespace) lança AP.PAY09.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Reject_WithEmptyReason_ShouldThrowDomainException(string reason)
        {
            var payable = PayableMother.AwaitingApproval();

            var ex = Assert.Throws<DomainException>(
                () => payable.Reject(PayableMother.DEFAULT_USER, reason, LATER));

            Assert.Equal("AP.PAY09", ex.Id);
        }

        // Reject em Draft lança AP.PAY01 (precisa estar em AwaitingApproval).
        [Fact]
        public void Reject_FromDraft_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.Classified();

            var ex = Assert.Throws<DomainException>(
                () => payable.Reject(PayableMother.DEFAULT_USER, "Motivo", LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }
    }

    public class WhenSchedulingWithApprovalThreshold
    {
        private static readonly Money LOW_THRESHOLD = new(1_000m, Currency.Brl);
        private static readonly Money HIGH_THRESHOLD = new(10_000m, Currency.Brl);

        // Valor abaixo do threshold pode ser agendado direto, sem RequestApproval.
        [Fact]
        public void Schedule_AmountBelowThreshold_ShouldSucceedWithoutApproval()
        {
            var payable = PayableMother.Classified(); // amount default = 1_500

            payable.Schedule(
                PayableMother.DEFAULT_SCHEDULED_FOR,
                LATER,
                approvalThreshold: HIGH_THRESHOLD);

            Assert.Equal(PayableStatus.Scheduled, payable.Status);
        }

        // Valor acima do threshold sem approval lança AP.PAY07.
        [Fact]
        public void Schedule_AmountAboveThreshold_WithoutApproval_ShouldThrowRequiresApproval()
        {
            var payable = PayableMother.Classified(); // 1_500 > 1_000

            var ex = Assert.Throws<DomainException>(() => payable.Schedule(
                PayableMother.DEFAULT_SCHEDULED_FOR,
                LATER,
                approvalThreshold: LOW_THRESHOLD));

            Assert.Equal("AP.PAY07", ex.Id);
        }

        // Valor acima do threshold com Status=Approved sucede (approval já consumido).
        [Fact]
        public void Schedule_AmountAboveThreshold_AfterApproval_ShouldSucceed()
        {
            var payable = PayableMother.Approved(); // 1_500, Status=Approved

            payable.Schedule(
                PayableMother.DEFAULT_SCHEDULED_FOR,
                LATER,
                approvalThreshold: LOW_THRESHOLD);

            Assert.Equal(PayableStatus.Scheduled, payable.Status);
        }

        // Valor igual ao threshold ainda passa (limite é "> threshold", inclusive).
        [Fact]
        public void Schedule_AmountEqualsThreshold_ShouldSucceedWithoutApproval()
        {
            var payable = PayableMother.Classified(amount: 1_000m);

            payable.Schedule(
                PayableMother.DEFAULT_SCHEDULED_FOR,
                LATER,
                approvalThreshold: LOW_THRESHOLD);

            Assert.Equal(PayableStatus.Scheduled, payable.Status);
        }
    }

    public class WhenMarkingAsPaidWithApprovalThreshold
    {
        private static readonly Money LOW_THRESHOLD = new(1_000m, Currency.Brl);

        // MarkAsPaidManually acima do threshold sem approval lança AP.PAY07.
        [Fact]
        public void MarkAsPaidManually_AmountAboveThreshold_WithoutApproval_ShouldThrowRequiresApproval()
        {
            var payable = PayableMother.Classified(); // 1_500 > 1_000

            var ex = Assert.Throws<DomainException>(() => payable.MarkAsPaidManually(
                PayableMother.DEFAULT_PROOF,
                paidAt: FIXED_NOW.AddDays(1),
                occurredAt: LATER,
                approvalThreshold: LOW_THRESHOLD));

            Assert.Equal("AP.PAY07", ex.Id);
        }

        // MarkAsPaidManually em Payable Approved acima do threshold sucede.
        [Fact]
        public void MarkAsPaidManually_AmountAboveThreshold_AfterApproval_ShouldSucceed()
        {
            var payable = PayableMother.Approved();

            payable.MarkAsPaidManually(
                PayableMother.DEFAULT_PROOF,
                paidAt: FIXED_NOW.AddDays(1),
                occurredAt: LATER,
                approvalThreshold: LOW_THRESHOLD);

            Assert.Equal(PayableStatus.Paid, payable.Status);
        }

        // MarkAsPaidManually em Payable Rejected lança AP.PAY01 (Rejected é terminal — critério da Sprint 5).
        [Fact]
        public void MarkAsPaidManually_OnRejected_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.Rejected();

            var ex = Assert.Throws<DomainException>(() => payable.MarkAsPaidManually(
                PayableMother.DEFAULT_PROOF,
                paidAt: FIXED_NOW.AddDays(1),
                occurredAt: LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }
    }

    public class WhenScheduling
    {
        // Schedule a partir de Draft classificado muda status para Scheduled, define ScheduledFor e emite PayableScheduled.
        [Fact]
        public void Schedule_FromClassifiedDraft_ShouldChangeStatusAndEmitEvent()
        {
            var payable = PayableMother.Classified();
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

        // Schedule em Draft NÃO classificado e sem allowUnclassified lança AP.PAY05 (invariante da Sprint 4).
        [Fact]
        public void Schedule_FromUnclassifiedDraft_WithoutAllowFlag_ShouldThrowDomainException()
        {
            var payable = PayableMother.Draft();

            var ex = Assert.Throws<DomainException>(
                () => payable.Schedule(PayableMother.DEFAULT_SCHEDULED_FOR, LATER));

            Assert.Equal("AP.PAY05", ex.Id);
        }

        // Schedule em Draft NÃO classificado com allowUnclassified=true sucede (bypass autorizado pelo setting do tenant).
        [Fact]
        public void Schedule_FromUnclassifiedDraft_WithAllowFlag_ShouldSucceed()
        {
            var payable = PayableMother.Draft();
            payable.PullChanges();

            payable.Schedule(PayableMother.DEFAULT_SCHEDULED_FOR, LATER, allowUnclassified: true);

            Assert.Equal(PayableStatus.Scheduled, payable.Status);
            Assert.IsType<PayableScheduled>(payable.Changes.Single());
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

    public class WhenRequestingPayment
    {
        // RequestPayment a partir de Scheduled muda status para PaymentRequested, popula método/conta/timestamp e emite PayablePaymentRequested.
        [Fact]
        public void RequestPayment_FromScheduled_ShouldChangeStatusAndEmitEvent()
        {
            var payable = PayableMother.Scheduled();
            payable.PullChanges();
            var occurredAt = FIXED_NOW.AddMinutes(20);

            payable.RequestPayment(PaymentMethod.Pix, PayableMother.DEFAULT_BANK_ACCOUNT, occurredAt);

            Assert.Equal(PayableStatus.PaymentRequested, payable.Status);
            Assert.Equal(PaymentMethod.Pix, payable.PaymentMethod);
            Assert.Equal(PayableMother.DEFAULT_BANK_ACCOUNT, payable.PaymentBankAccountId);
            Assert.Equal(occurredAt, payable.PaymentRequestedAt);
            var requested = Assert.IsType<PayablePaymentRequested>(payable.Changes.Single());
            Assert.Equal(payable.Id, requested.PayableId);
            Assert.Equal(PayableMother.DEFAULT_SUPPLIER, requested.SupplierId);
            Assert.Equal(1_500m, requested.AmountValue);
            Assert.Equal("BRL", requested.AmountCurrency);
            Assert.Equal(PayableMother.DEFAULT_BANK_ACCOUNT, requested.BankAccountId);
            Assert.Equal("PIX", requested.Method);
        }

        // RequestPayment em Draft/Approved/AwaitingApproval/Paid/Cancelled/Rejected lança AP.PAY01 (só sai de Scheduled ou PaymentFailed).
        [Theory]
        [InlineData("Draft")]
        [InlineData("AwaitingApproval")]
        [InlineData("Approved")]
        [InlineData("Paid")]
        [InlineData("Cancelled")]
        [InlineData("Rejected")]
        public void RequestPayment_FromInvalidStatus_ShouldThrowInvalidStatusTransition(string state)
        {
            var payable = state switch
            {
                "Draft" => PayableMother.Draft(),
                "AwaitingApproval" => PayableMother.AwaitingApproval(),
                "Approved" => PayableMother.Approved(),
                "Paid" => PayableMother.Paid(),
                "Cancelled" => PayableMother.Cancelled(),
                "Rejected" => PayableMother.Rejected(),
                _ => throw new InvalidOperationException()
            };

            var ex = Assert.Throws<DomainException>(() => payable.RequestPayment(
                PaymentMethod.Pix, PayableMother.DEFAULT_BANK_ACCOUNT, LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }

        // RequestPayment com PaymentMethod null lança ArgumentNullException (guarda básica antes da máquina de estados).
        [Fact]
        public void RequestPayment_WithNullMethod_ShouldThrowArgumentNullException()
        {
            var payable = PayableMother.Scheduled();

            Assert.Throws<ArgumentNullException>(() => payable.RequestPayment(
                null!, PayableMother.DEFAULT_BANK_ACCOUNT, LATER));
        }

        // Critério de aceite Sprint 6: RequestPayment numa Payable não aprovada (acima do threshold) falha com AP.PAY07.
        [Fact]
        public void RequestPayment_AmountAboveThreshold_WithoutApproval_ShouldThrowRequiresApproval()
        {
            // Schedule sem threshold, depois RequestPayment com threshold mais baixo que o amount.
            var payable = PayableMother.Scheduled(); // amount default 1_500, ApprovedAt = null
            var lowThreshold = new Money(1_000m, Currency.Brl);

            var ex = Assert.Throws<DomainException>(() => payable.RequestPayment(
                PaymentMethod.Pix, PayableMother.DEFAULT_BANK_ACCOUNT, LATER, approvalThreshold: lowThreshold));

            Assert.Equal("AP.PAY07", ex.Id);
        }

        // RequestPayment numa Payable que já passou por Approval (ApprovedAt set) sucede mesmo acima do threshold — approval consumida.
        [Fact]
        public void RequestPayment_AmountAboveThreshold_AfterApproval_ShouldSucceed()
        {
            // Fluxo completo: Classified → AwaitingApproval → Approved → Scheduled → RequestPayment.
            var payable = PayableMother.Approved();
            payable.Schedule(PayableMother.DEFAULT_SCHEDULED_FOR, FIXED_NOW.AddMinutes(6));
            var lowThreshold = new Money(1_000m, Currency.Brl);

            payable.RequestPayment(PaymentMethod.Ted, PayableMother.DEFAULT_BANK_ACCOUNT, LATER, approvalThreshold: lowThreshold);

            Assert.Equal(PayableStatus.PaymentRequested, payable.Status);
            Assert.NotNull(payable.ApprovedAt); // sanity — approval persistiu
        }

        // RequestPayment a partir de PaymentFailed sucede (retry após bank rejeitar) — limpa motivo da falha anterior.
        [Fact]
        public void RequestPayment_FromPaymentFailed_ShouldSucceedAndClearFailureFields()
        {
            var payable = PayableMother.PaymentFailed();
            payable.PullChanges();
            Assert.Equal("Conta destino inválida", payable.PaymentFailureReason); // sanity

            payable.RequestPayment(PaymentMethod.Pix, PayableMother.DEFAULT_BANK_ACCOUNT, LATER);

            Assert.Equal(PayableStatus.PaymentRequested, payable.Status);
            Assert.Null(payable.PaymentFailureReason);
            Assert.Null(payable.PaymentFailedAt);
            Assert.IsType<PayablePaymentRequested>(payable.Changes.Single());
        }
    }

    public class WhenConfirmingPaid
    {
        // ConfirmPaid a partir de PaymentRequested muda status para Paid, registra PaymentOrderId e PaidAt, e emite PayablePaid.
        [Fact]
        public void ConfirmPaid_FromPaymentRequested_ShouldChangeStatusAndEmitEvent()
        {
            var payable = PayableMother.PaymentRequested();
            payable.PullChanges();
            var paidAt = FIXED_NOW.AddDays(1);

            payable.ConfirmPaid(PayableMother.DEFAULT_PAYMENT_ORDER, paidAt, paidAt.AddMinutes(1));

            Assert.Equal(PayableStatus.Paid, payable.Status);
            Assert.Equal(PayableMother.DEFAULT_PAYMENT_ORDER, payable.LastPaymentOrderId);
            Assert.Equal(paidAt, payable.PaidAt);
            var paid = Assert.IsType<PayablePaid>(payable.Changes.Single());
            Assert.Equal(payable.Id, paid.PayableId);
            Assert.Equal(PayableMother.DEFAULT_PAYMENT_ORDER, paid.PaymentOrderId);
            Assert.Equal(paidAt, paid.PaidAt);
        }

        // Critério de aceite Sprint 6: ConfirmPaid numa Payable que não pediu pagamento (Scheduled, Draft, etc.) falha com AP.PAY01.
        [Theory]
        [InlineData("Draft")]
        [InlineData("Scheduled")]
        [InlineData("AwaitingApproval")]
        public void ConfirmPaid_OnPayableThatDidNotRequestPayment_ShouldThrowInvalidStatusTransition(string state)
        {
            var payable = state switch
            {
                "Draft" => PayableMother.Draft(),
                "Scheduled" => PayableMother.Scheduled(),
                "AwaitingApproval" => PayableMother.AwaitingApproval(),
                _ => throw new InvalidOperationException()
            };

            var ex = Assert.Throws<DomainException>(() => payable.ConfirmPaid(
                PayableMother.DEFAULT_PAYMENT_ORDER, FIXED_NOW.AddDays(1), LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }

        // Critério de aceite Sprint 6: receber PaymentOrderExecuted duas vezes com mesmo PaymentOrderId é no-op (idempotência).
        [Fact]
        public void ConfirmPaid_SamePaymentOrderTwice_ShouldBeIdempotent()
        {
            var payable = PayableMother.PaymentRequested();
            var paidAt = FIXED_NOW.AddDays(1);
            payable.ConfirmPaid(PayableMother.DEFAULT_PAYMENT_ORDER, paidAt, paidAt.AddMinutes(1));
            payable.PullChanges();

            payable.ConfirmPaid(PayableMother.DEFAULT_PAYMENT_ORDER, paidAt, paidAt.AddMinutes(2));

            Assert.Empty(payable.Changes); // segundo ConfirmPaid não emite evento
            Assert.Equal(PayableStatus.Paid, payable.Status);
            Assert.Equal(paidAt, payable.PaidAt); // PaidAt mantém o primeiro valor
        }

        // ConfirmPaid numa Payable já paga, mas com PaymentOrderId diferente, lança AP.PAY11 (callback órfão).
        [Fact]
        public void ConfirmPaid_OnPaidPayableWithDifferentPaymentOrder_ShouldThrowMismatch()
        {
            var payable = PayableMother.PaymentRequested();
            payable.ConfirmPaid(PayableMother.DEFAULT_PAYMENT_ORDER, FIXED_NOW.AddDays(1), LATER);
            var otherPaymentOrder = PaymentOrderId.From(new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));

            var ex = Assert.Throws<DomainException>(() => payable.ConfirmPaid(
                otherPaymentOrder, FIXED_NOW.AddDays(2), LATER.AddDays(1)));

            Assert.Equal("AP.PAY11", ex.Id);
        }
    }

    public class WhenMarkingPaymentFailed
    {
        // MarkPaymentFailed a partir de PaymentRequested muda status, registra PaymentOrderId/reason/timestamp e emite PayablePaymentFailed.
        [Fact]
        public void MarkPaymentFailed_FromPaymentRequested_ShouldChangeStatusAndEmitEvent()
        {
            var payable = PayableMother.PaymentRequested();
            payable.PullChanges();
            var occurredAt = FIXED_NOW.AddMinutes(30);

            payable.MarkPaymentFailed(PayableMother.DEFAULT_PAYMENT_ORDER, "Saldo insuficiente", occurredAt);

            Assert.Equal(PayableStatus.PaymentFailed, payable.Status);
            Assert.Equal(PayableMother.DEFAULT_PAYMENT_ORDER, payable.LastPaymentOrderId);
            Assert.Equal(occurredAt, payable.PaymentFailedAt);
            Assert.Equal("Saldo insuficiente", payable.PaymentFailureReason);
            var failed = Assert.IsType<PayablePaymentFailed>(payable.Changes.Single());
            Assert.Equal(payable.Id, failed.PayableId);
            Assert.Equal(PayableMother.DEFAULT_PAYMENT_ORDER, failed.PaymentOrderId);
            Assert.Equal("Saldo insuficiente", failed.Reason);
        }

        // MarkPaymentFailed em Scheduled (sem RequestPayment prévio) lança AP.PAY01.
        [Fact]
        public void MarkPaymentFailed_FromScheduled_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.Scheduled();

            var ex = Assert.Throws<DomainException>(() => payable.MarkPaymentFailed(
                PayableMother.DEFAULT_PAYMENT_ORDER, "qualquer", LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }

        // MarkPaymentFailed com reason vazio/whitespace lança AP.PAY10 (validação anterior à máquina de estados).
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void MarkPaymentFailed_WithEmptyReason_ShouldThrowPaymentFailureReasonRequired(string reason)
        {
            var payable = PayableMother.PaymentRequested();

            var ex = Assert.Throws<DomainException>(() => payable.MarkPaymentFailed(
                PayableMother.DEFAULT_PAYMENT_ORDER, reason, LATER));

            Assert.Equal("AP.PAY10", ex.Id);
        }
    }

    public class WhenCancellingAfterPaymentSprint6
    {
        // Cancel a partir de PaymentFailed sucede (estado não-terminal — Sprint 6).
        [Fact]
        public void Cancel_FromPaymentFailed_ShouldChangeStatusToCancelled()
        {
            var payable = PayableMother.PaymentFailed();
            payable.PullChanges();

            payable.Cancel("Desistência após falha", LATER);

            Assert.Equal(PayableStatus.Cancelled, payable.Status);
            var cancelled = Assert.IsType<PayableCancelled>(payable.Changes.Single());
            Assert.Equal("Desistência após falha", cancelled.Reason);
        }

        // Cancel a partir de PaymentRequested lança AP.PAY01 — precisa primeiro falhar/concluir (evita PaymentOrder pendente órfã).
        [Fact]
        public void Cancel_FromPaymentRequested_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.PaymentRequested();

            var ex = Assert.Throws<DomainException>(() => payable.Cancel("motivo", LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }
    }

    public class WhenRetryingAfterFailure
    {
        // Fluxo completo de retry (critério de aceite Sprint 6 + máquina de estados):
        // Scheduled → RequestPayment → PaymentRequested → MarkPaymentFailed → PaymentFailed → RequestPayment → PaymentRequested → ConfirmPaid → Paid.
        [Fact]
        public void RetryFlow_AfterFailure_ShouldEventuallyReachPaid()
        {
            var payable = PayableMother.Scheduled();

            payable.RequestPayment(PaymentMethod.Pix, PayableMother.DEFAULT_BANK_ACCOUNT, FIXED_NOW.AddMinutes(10));
            payable.MarkPaymentFailed(PayableMother.DEFAULT_PAYMENT_ORDER, "Saldo insuficiente", FIXED_NOW.AddMinutes(20));
            payable.RequestPayment(PaymentMethod.Ted, PayableMother.DEFAULT_BANK_ACCOUNT, FIXED_NOW.AddMinutes(30));
            var newPaymentOrder = PaymentOrderId.From(new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
            payable.ConfirmPaid(newPaymentOrder, FIXED_NOW.AddMinutes(40), FIXED_NOW.AddMinutes(41));

            Assert.Equal(PayableStatus.Paid, payable.Status);
            Assert.Equal(newPaymentOrder, payable.LastPaymentOrderId);
            Assert.Equal(PaymentMethod.Ted, payable.PaymentMethod);
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

            Assert.Equal(3, pulled.Count); // PayableCreated + PayableClassified + PayableScheduled
            Assert.Empty(payable.Changes);
            Assert.Equal(3, payable.Version);
            Assert.Equal(PayableStatus.Scheduled, payable.Status);
        }
    }
}
