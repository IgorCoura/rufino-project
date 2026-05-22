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

    public class WhenInitializingAsInstallment
    {
        // InitializeAsInstallment cria Payable em Draft com InstallmentPlanId + InstallmentNumber populados (Sprint 8).
        [Fact]
        public void InitializeAsInstallment_WithValidData_ShouldCreateDraftLinkedToPlan()
        {
            var payable = PayableMother.DraftAsInstallment(installmentNumber: 3);

            Assert.Equal(PayableStatus.Draft, payable.Status);
            Assert.Equal(1, payable.Version);
            Assert.Equal(PayableMother.DEFAULT_INSTALLMENT_PLAN, payable.InstallmentPlanId);
            Assert.Equal(3, payable.InstallmentNumber);
            Assert.Null(payable.CapturedBillId);
        }

        // InitializeAsInstallment registra PayableCreatedAsInstallment carregando o link com o plano.
        [Fact]
        public void InitializeAsInstallment_ShouldRecordPayableCreatedAsInstallmentInChanges()
        {
            var payable = PayableMother.DraftAsInstallment(installmentNumber: 2);

            var change = Assert.Single(payable.Changes);
            var created = Assert.IsType<PayableCreatedAsInstallment>(change);
            Assert.Equal(payable.Id, created.PayableId);
            Assert.Equal(PayableMother.DEFAULT_INSTALLMENT_PLAN, created.InstallmentPlanId);
            Assert.Equal(2, created.InstallmentNumber);
            Assert.Equal(1_000m, created.AmountValue);
        }

        // InitializeAsInstallment com installmentNumber inválido (< 1) lança AP.PAY12.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void InitializeAsInstallment_WithNonPositiveInstallmentNumber_ShouldThrowDomainException(int installmentNumber)
        {
            var ex = Assert.Throws<DomainException>(() => PayableMother.DraftAsInstallment(installmentNumber: installmentNumber));

            Assert.Equal("AP.PAY12", ex.Id);
        }

        // InitializeAsInstallment com DueDate em passado lança AP.PAY02 (mesma guarda do Initialize regular).
        [Fact]
        public void InitializeAsInstallment_WithDueDateInPast_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => PayableMother.DraftAsInstallment(
                dueDate: new DateOnly(2023, 1, 1),
                occurredAt: FIXED_NOW));

            Assert.Equal("AP.PAY02", ex.Id);
        }

        // Cancelar uma parcela individual é permitido — afeta só aquela Payable (critério Sprint 8).
        [Fact]
        public void CancelSingleInstallment_ShouldNotAffectOtherPayablesOrThePlan()
        {
            var p2 = PayableMother.DraftAsInstallment(installmentNumber: 2);

            p2.Cancel("Cliente quitou antecipadamente esta parcela", LATER);

            Assert.Equal(PayableStatus.Cancelled, p2.Status);
            Assert.Equal(2, p2.InstallmentNumber);
            Assert.Equal(PayableMother.DEFAULT_INSTALLMENT_PLAN, p2.InstallmentPlanId); // link preservado
        }
    }

    public class WhenInitializingFromCapture
    {
        // InitializeFromCapture cria Payable em Draft com CapturedBillId populado e sem classificação (vai pra revisão humana — critério de aceite Sprint 7).
        [Fact]
        public void InitializeFromCapture_WithValidData_ShouldCreateDraftLinkedToCapturedBill()
        {
            var payable = PayableMother.DraftFromCapture();

            Assert.Equal(PayableStatus.Draft, payable.Status);
            Assert.Equal(1, payable.Version);
            Assert.Equal(PayableMother.DEFAULT_CAPTURED_BILL, payable.CapturedBillId);
            Assert.Equal(PayableMother.DEFAULT_TENANT, payable.TenantId);
            Assert.Equal(PayableMother.DEFAULT_SUPPLIER, payable.SupplierId);
            Assert.Equal(1_500m, payable.Amount.Amount);
            Assert.Equal("Boleto Sabesp março", payable.Description.Value);
            // Critério de aceite: estado inicial sem classificação até auto-classificação rodar (Sprint 9).
            Assert.Null(payable.Classification);
            Assert.Null(payable.CostCenterId);
        }

        // InitializeFromCapture registra PayableCreatedFromCapture em Changes carregando o CapturedBillId.
        [Fact]
        public void InitializeFromCapture_ShouldRecordPayableCreatedFromCaptureInChanges()
        {
            var payable = PayableMother.DraftFromCapture();

            var change = Assert.Single(payable.Changes);
            var created = Assert.IsType<PayableCreatedFromCapture>(change);
            Assert.Equal(payable.Id, created.PayableId);
            Assert.Equal(PayableMother.DEFAULT_CAPTURED_BILL, created.CapturedBillId);
            Assert.Equal(PayableMother.DEFAULT_SUPPLIER, created.SupplierId);
            Assert.Equal(1_500m, created.AmountValue);
            Assert.Equal("BRL", created.AmountCurrency);
            Assert.Equal(PayableMother.DEFAULT_DUE_DATE, created.DueDate);
            Assert.Equal("Boleto Sabesp março", created.Description);
        }

        // InitializeFromCapture com DueDate anterior ao OccurredAt rejeita igual ao Initialize regular (AP.PAY02).
        [Fact]
        public void InitializeFromCapture_WithDueDateInPast_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => PayableMother.DraftFromCapture(
                dueDate: new DateOnly(2023, 1, 1),
                occurredAt: FIXED_NOW));

            Assert.Equal("AP.PAY02", ex.Id);
        }

        // Payable criado a partir de captura segue o mesmo ciclo de vida — Classify → Schedule → MarkAsPaidManually deve funcionar normalmente.
        [Fact]
        public void DraftFromCapture_ShouldFlowThroughNormalLifecycle()
        {
            var payable = PayableMother.DraftFromCapture();

            payable.Classify(PayableMother.DEFAULT_ACCOUNT_REF, PayableMother.DEFAULT_COST_CENTER, PayableMother.DEFAULT_USER, LATER);
            payable.Schedule(PayableMother.DEFAULT_SCHEDULED_FOR, LATER.AddMinutes(1));
            payable.MarkAsPaidManually(PayableMother.DEFAULT_PROOF, FIXED_NOW.AddDays(30), FIXED_NOW.AddDays(30).AddMinutes(1));

            Assert.Equal(PayableStatus.Paid, payable.Status);
            Assert.Equal(PayableMother.DEFAULT_CAPTURED_BILL, payable.CapturedBillId); // CapturedBillId sobrevive
        }

        // Rehydrate a partir de stream começando com PayableCreatedFromCapture reconstrói CapturedBillId corretamente.
        [Fact]
        public void Rehydrate_FromCaptureStream_ShouldRebuildCapturedBillIdLink()
        {
            var payableId = PayableId.From(new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));
            var history = new IDomainEvent[]
            {
                PayableMother.TEMPLATE_PAYABLE_CREATED_FROM_CAPTURE with
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = FIXED_NOW,
                    PayableId = payableId,
                    Description = "Boleto Sabesp março",
                },
            };

            var payable = Payable.Rehydrate(history);

            Assert.Equal(payableId, payable.Id);
            Assert.Equal(PayableStatus.Draft, payable.Status);
            Assert.Equal(PayableMother.DEFAULT_CAPTURED_BILL, payable.CapturedBillId);
            Assert.Equal(1, payable.Version);
            Assert.Empty(payable.Changes);
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
                PayableMother.DEFAULT_ACCOUNT_REF,
                PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_USER,
                LATER);

            Assert.Equal(PayableMother.DEFAULT_ACCOUNT_REF, payable.Classification);
            Assert.Equal(PayableMother.DEFAULT_COST_CENTER, payable.CostCenterId);
            Assert.Equal(PayableMother.DEFAULT_USER, payable.ClassifiedBy);
            Assert.Equal(LATER, payable.ClassifiedAt);
            var classified = Assert.IsType<PayableClassified>(payable.Changes.Single());
            Assert.Equal(payable.Id, classified.PayableId);
            Assert.Equal(PayableMother.DEFAULT_CHART_OF_ACCOUNTS, classified.ChartOfAccountsId);
            Assert.Equal(PayableMother.DEFAULT_ACCOUNT, classified.AccountId);
        }

        // Reclassify em Draft já classificado é permitido — emite novo PayableClassified (A+ES preserva histórico).
        [Fact]
        public void Classify_OnAlreadyClassifiedDraft_ShouldEmitNewEvent()
        {
            var payable = PayableMother.Classified();
            payable.PullChanges();
            var newRef = new AccountRef(
                PayableMother.DEFAULT_CHART_OF_ACCOUNTS,
                AccountId.From(new Guid("88888888-8888-8888-8888-888888888888")));

            payable.Classify(newRef, PayableMother.DEFAULT_COST_CENTER, PayableMother.DEFAULT_USER, LATER);

            Assert.Equal(newRef, payable.Classification);
            var reclassified = Assert.IsType<PayableClassified>(payable.Changes.Single());
            Assert.Equal(newRef.AccountId, reclassified.AccountId);
            Assert.Equal(newRef.ChartOfAccountsId, reclassified.ChartOfAccountsId);
        }

        // Reclassify em Scheduled é permitido (Status não-terminal).
        [Fact]
        public void Classify_OnScheduled_ShouldSucceed()
        {
            var payable = PayableMother.Scheduled();
            payable.PullChanges();
            var newCostCenter = CostCenterId.From(new Guid("77777777-7777-7777-7777-777777777777"));

            payable.Classify(PayableMother.DEFAULT_ACCOUNT_REF, newCostCenter, PayableMother.DEFAULT_USER, LATER);

            Assert.Equal(newCostCenter, payable.CostCenterId);
            Assert.Equal(PayableStatus.Scheduled, payable.Status); // status permanece
        }

        // Classify em Payable Paid (terminal) lança AP.PAY06.
        [Fact]
        public void Classify_OnPaid_ShouldThrowDomainException()
        {
            var payable = PayableMother.Paid();

            var ex = Assert.Throws<DomainException>(() => payable.Classify(
                PayableMother.DEFAULT_ACCOUNT_REF, PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_USER, LATER));

            Assert.Equal("AP.PAY06", ex.Id);
        }

        // Classify em Payable Cancelled (terminal) lança AP.PAY06.
        [Fact]
        public void Classify_OnCancelled_ShouldThrowDomainException()
        {
            var payable = PayableMother.Cancelled();

            var ex = Assert.Throws<DomainException>(() => payable.Classify(
                PayableMother.DEFAULT_ACCOUNT_REF, PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_USER, LATER));

            Assert.Equal("AP.PAY06", ex.Id);
        }
    }

    public class WhenClassifyingAutomatically
    {
        // ClassifyAutomatically em Draft popula AccountId/CostCenterId/LastClassificationRuleId e deixa ClassifiedBy null (Sprint 9).
        [Fact]
        public void ClassifyAutomatically_FromDraft_ShouldPopulateStateAndEmitEvent()
        {
            var payable = PayableMother.Draft();
            payable.PullChanges();

            payable.ClassifyAutomatically(
                PayableMother.DEFAULT_ACCOUNT_REF,
                PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_RULE,
                LATER);

            Assert.Equal(PayableMother.DEFAULT_ACCOUNT_REF, payable.Classification);
            Assert.Equal(PayableMother.DEFAULT_COST_CENTER, payable.CostCenterId);
            Assert.Equal(PayableMother.DEFAULT_RULE, payable.LastClassificationRuleId);
            Assert.Null(payable.ClassifiedBy); // automática — sem usuário
            Assert.Equal(LATER, payable.ClassifiedAt);
            var autoClassified = Assert.IsType<PayableAutoClassified>(payable.Changes.Single());
            Assert.Equal(PayableMother.DEFAULT_RULE, autoClassified.RuleId);
            Assert.Equal(PayableMother.DEFAULT_CHART_OF_ACCOUNTS, autoClassified.ChartOfAccountsId);
            Assert.Equal(PayableMother.DEFAULT_ACCOUNT, autoClassified.AccountId);
        }

        // Reclassificação manual após classificação automática limpa LastClassificationRuleId — humano sobrescreveu a regra.
        [Fact]
        public void ManualClassify_AfterAutoClassify_ShouldClearRuleIdAndRecordClassifier()
        {
            var payable = PayableMother.AutoClassified();
            payable.PullChanges();

            payable.Classify(
                PayableMother.DEFAULT_ACCOUNT_REF,
                PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_USER,
                LATER);

            Assert.Null(payable.LastClassificationRuleId);
            Assert.Equal(PayableMother.DEFAULT_USER, payable.ClassifiedBy);
        }

        // ClassifyAutomatically em Payable Paid (terminal) lança AP.PAY06 — mesma invariante de Classify.
        [Fact]
        public void ClassifyAutomatically_OnPaid_ShouldThrowDomainException()
        {
            var payable = PayableMother.Paid();

            var ex = Assert.Throws<DomainException>(() => payable.ClassifyAutomatically(
                PayableMother.DEFAULT_ACCOUNT_REF,
                PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_RULE,
                LATER));

            Assert.Equal("AP.PAY06", ex.Id);
        }

        // ClassifyAutomatically em Payable Cancelled (terminal) lança AP.PAY06.
        [Fact]
        public void ClassifyAutomatically_OnCancelled_ShouldThrowDomainException()
        {
            var payable = PayableMother.Cancelled();

            var ex = Assert.Throws<DomainException>(() => payable.ClassifyAutomatically(
                PayableMother.DEFAULT_ACCOUNT_REF,
                PayableMother.DEFAULT_COST_CENTER,
                PayableMother.DEFAULT_RULE,
                LATER));

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

    public class WhenRequestingMultiApproval
    {
        // RequestMultiApproval em Draft classificado vai pra AwaitingApproval, popula RequiredApprovalCount/EligibleRoles e emite PayableMultiApprovalRequested.
        [Fact]
        public void RequestMultiApproval_FromClassifiedDraft_ShouldSetUpMultiModeAndEmitEvent()
        {
            var payable = PayableMother.Classified();
            payable.PullChanges();

            payable.RequestMultiApproval(requiredCount: 2, eligibleRoles: new[] { "OWNER", "PARTNER" }, LATER);

            Assert.Equal(PayableStatus.AwaitingApproval, payable.Status);
            Assert.True(payable.IsMultiApproval);
            Assert.Equal(2, payable.RequiredApprovalCount);
            Assert.Equal(new[] { "OWNER", "PARTNER" }, payable.EligibleApproverRoles);
            Assert.Empty(payable.ApprovalsReceived);
            var requested = Assert.IsType<PayableMultiApprovalRequested>(payable.Changes.Single());
            Assert.Equal(2, requested.RequiredApprovalCount);
            Assert.Equal(new[] { "OWNER", "PARTNER" }, requested.EligibleApproverRoles);
        }

        // RequestMultiApproval com requiredCount < 1 lança AP.PAY13.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void RequestMultiApproval_WithNonPositiveCount_ShouldThrowDomainException(int count)
        {
            var payable = PayableMother.Classified();

            var ex = Assert.Throws<DomainException>(() => payable.RequestMultiApproval(
                count, new[] { "OWNER" }, LATER));

            Assert.Equal("AP.PAY13", ex.Id);
        }

        // RequestMultiApproval com eligibleRoles vazio lança AP.PAY14.
        [Fact]
        public void RequestMultiApproval_WithEmptyRoles_ShouldThrowDomainException()
        {
            var payable = PayableMother.Classified();

            var ex = Assert.Throws<DomainException>(() => payable.RequestMultiApproval(
                requiredCount: 2, Array.Empty<string>(), LATER));

            Assert.Equal("AP.PAY14", ex.Id);
        }

        // RequestMultiApproval em Draft não classificado lança AP.PAY08 (mesma invariante de RequestApproval).
        [Fact]
        public void RequestMultiApproval_FromUnclassifiedDraft_ShouldThrowDomainException()
        {
            var payable = PayableMother.Draft();

            var ex = Assert.Throws<DomainException>(() => payable.RequestMultiApproval(
                2, new[] { "OWNER" }, LATER));

            Assert.Equal("AP.PAY08", ex.Id);
        }
    }

    public class WhenRecordingApprovals
    {
        private static Payable BuildAwaitingMultiApproval(int requiredCount = 2)
        {
            var payable = PayableMother.Classified();
            payable.RequestMultiApproval(requiredCount, new[] { "OWNER", "PARTNER" }, LATER);
            payable.PullChanges();
            return payable;
        }

        // RecordApproval primeira vez registra ApprovalRecord e emite PayableApprovalRecorded — status continua AwaitingApproval.
        [Fact]
        public void RecordApproval_BeforeCountReached_ShouldRecordAndStayAwaiting()
        {
            var payable = BuildAwaitingMultiApproval(requiredCount: 2);
            var u1 = UserId.From(new Guid("11111111-1111-1111-1111-aaaaaaaaaaaa"));

            payable.RecordApproval(u1, "owner", LATER.AddMinutes(1));

            Assert.Equal(PayableStatus.AwaitingApproval, payable.Status);
            Assert.Single(payable.ApprovalsReceived);
            Assert.Equal("OWNER", payable.ApprovalsReceived[0].Role); // role normalizada
            var recorded = Assert.IsType<PayableApprovalRecorded>(payable.Changes.Single());
            Assert.Equal(u1, recorded.ApprovedBy);
        }

        // RecordApproval que completa a contagem emite PayableApprovalRecorded + PayableFullyApproved e status vira Approved.
        [Fact]
        public void RecordApproval_WhenLastSlotFilled_ShouldEmitFullyApprovedAndFlipStatus()
        {
            var payable = BuildAwaitingMultiApproval(requiredCount: 2);
            var u1 = UserId.From(new Guid("11111111-1111-1111-1111-aaaaaaaaaaaa"));
            var u2 = UserId.From(new Guid("22222222-2222-2222-2222-aaaaaaaaaaaa"));
            payable.RecordApproval(u1, "OWNER", LATER.AddMinutes(1));
            payable.PullChanges();

            payable.RecordApproval(u2, "PARTNER", LATER.AddMinutes(2));

            Assert.Equal(PayableStatus.Approved, payable.Status);
            Assert.Equal(2, payable.ApprovalsReceived.Count);
            var changes = payable.Changes.ToList();
            Assert.Equal(2, changes.Count);
            Assert.IsType<PayableApprovalRecorded>(changes[0]);
            Assert.IsType<PayableFullyApproved>(changes[1]);
            Assert.Equal(LATER.AddMinutes(2), payable.ApprovedAt);
        }

        // RecordApproval com role fora de EligibleApproverRoles lança AP.PAY15.
        [Fact]
        public void RecordApproval_WithIneligibleRole_ShouldThrowDomainException()
        {
            var payable = BuildAwaitingMultiApproval();

            var ex = Assert.Throws<DomainException>(() => payable.RecordApproval(
                UserId.From(new Guid("11111111-1111-1111-1111-aaaaaaaaaaaa")), "FINANCE", LATER));

            Assert.Equal("AP.PAY15", ex.Id);
        }

        // RecordApproval do mesmo usuário duas vezes lança AP.PAY16 (não conta como 2 aprovações distintas).
        [Fact]
        public void RecordApproval_TwiceFromSameUser_ShouldThrowDomainException()
        {
            var payable = BuildAwaitingMultiApproval();
            var user = UserId.From(new Guid("11111111-1111-1111-1111-aaaaaaaaaaaa"));
            payable.RecordApproval(user, "OWNER", LATER.AddMinutes(1));

            var ex = Assert.Throws<DomainException>(() => payable.RecordApproval(
                user, "PARTNER", LATER.AddMinutes(2)));

            Assert.Equal("AP.PAY16", ex.Id);
        }

        // Em multi-mode, chamar Approve(UserId, DateTime) lança AP.PAY17 — proteção contra mistura de fluxos.
        [Fact]
        public void Approve_InMultiMode_ShouldThrowDomainException()
        {
            var payable = BuildAwaitingMultiApproval();

            var ex = Assert.Throws<DomainException>(() => payable.Approve(
                UserId.From(new Guid("11111111-1111-1111-1111-aaaaaaaaaaaa")), LATER));

            Assert.Equal("AP.PAY17", ex.Id);
        }

        // RecordApproval em Payable que não está em multi-mode lança AP.PAY01.
        [Fact]
        public void RecordApproval_OutsideMultiMode_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.Classified(); // sem multi-aprovação iniciada

            var ex = Assert.Throws<DomainException>(() => payable.RecordApproval(
                UserId.From(new Guid("11111111-1111-1111-1111-aaaaaaaaaaaa")), "OWNER", LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }
    }

    public class WhenSchedulingApprovalGating
    {
        // Após Approve, Schedule sucede — Approved → Scheduled é a transição esperada do fluxo
        // single-approver: orquestrador chama RequestApproval/Approve antes do Schedule.
        [Fact]
        public void Schedule_FromApproved_ShouldSucceed()
        {
            var payable = PayableMother.Approved();

            payable.Schedule(PayableMother.DEFAULT_SCHEDULED_FOR, LATER);

            Assert.Equal(PayableStatus.Scheduled, payable.Status);
        }

        // Schedule a partir de AwaitingApproval é bloqueado pela máquina de estados (AP.PAY01) —
        // é o mecanismo que substituiu o antigo check de threshold dentro do Aggregate.
        [Fact]
        public void Schedule_FromAwaitingApproval_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.AwaitingApproval();

            var ex = Assert.Throws<DomainException>(() => payable.Schedule(
                PayableMother.DEFAULT_SCHEDULED_FOR, LATER));

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

        // MarkAsPaidManually em Payable AwaitingApproval é bloqueado pela máquina de estados (AP.PAY01) —
        // substitui o antigo check de threshold; orquestrador deve passar por Approve antes.
        [Fact]
        public void MarkAsPaidManually_OnAwaitingApproval_ShouldThrowInvalidStatusTransition()
        {
            var payable = PayableMother.AwaitingApproval();

            var ex = Assert.Throws<DomainException>(() => payable.MarkAsPaidManually(
                PayableMother.DEFAULT_PROOF,
                paidAt: FIXED_NOW.AddDays(1),
                occurredAt: LATER));

            Assert.Equal("AP.PAY01", ex.Id);
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

            payable.RequestPayment(occurredAt);

            Assert.Equal(PayableStatus.PaymentRequested, payable.Status);
            Assert.Equal(PaymentMethod.SupplierTransfer, payable.PaymentInstrument.Method);
            Assert.Equal(occurredAt, payable.PaymentRequestedAt);
            var requested = Assert.IsType<PayablePaymentRequested>(payable.Changes.Single());
            Assert.Equal(payable.Id, requested.PayableId);
            Assert.Equal(PayableMother.DEFAULT_SUPPLIER, requested.SupplierId);
            Assert.Equal(1_500m, requested.AmountValue);
            Assert.Equal("BRL", requested.AmountCurrency);
            Assert.Equal("SUPPLIER_BANK", requested.InstrumentKind);
            Assert.Equal("001", requested.BankCode);
            Assert.Equal("123456-7", requested.AccountNumber);
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

            var ex = Assert.Throws<DomainException>(() => payable.RequestPayment(LATER));

            Assert.Equal("AP.PAY01", ex.Id);
        }

        // Fluxo completo Classified → AwaitingApproval → Approved → Scheduled → RequestPayment chega em PaymentRequested.
        // Substitui o antigo teste de "above threshold after approval": a gating é a transição, não um Money? na chamada.
        [Fact]
        public void RequestPayment_AfterApprovalAndSchedule_ShouldSucceed()
        {
            var payable = PayableMother.Approved();
            payable.Schedule(PayableMother.DEFAULT_SCHEDULED_FOR, FIXED_NOW.AddMinutes(6));

            payable.RequestPayment(LATER);

            Assert.Equal(PayableStatus.PaymentRequested, payable.Status);
            Assert.NotNull(payable.ApprovedAt);
        }

        // RequestPayment a partir de PaymentFailed sucede (retry após bank rejeitar) — limpa motivo da falha anterior.
        [Fact]
        public void RequestPayment_FromPaymentFailed_ShouldSucceedAndClearFailureFields()
        {
            var payable = PayableMother.PaymentFailed();
            payable.PullChanges();
            Assert.Equal("Conta destino inválida", payable.PaymentFailureReason); // sanity

            payable.RequestPayment(LATER);

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

            payable.RequestPayment(FIXED_NOW.AddMinutes(10));
            payable.MarkPaymentFailed(PayableMother.DEFAULT_PAYMENT_ORDER, "Saldo insuficiente", FIXED_NOW.AddMinutes(20));
            payable.RequestPayment(FIXED_NOW.AddMinutes(30));
            var newPaymentOrder = PaymentOrderId.From(new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
            payable.ConfirmPaid(newPaymentOrder, FIXED_NOW.AddMinutes(40), FIXED_NOW.AddMinutes(41));

            Assert.Equal(PayableStatus.Paid, payable.Status);
            Assert.Equal(newPaymentOrder, payable.LastPaymentOrderId);
            // PaymentMethod foi decidido na criação (Sprint 12.B) — segundo RequestPayment não muda.
            Assert.Equal(PaymentMethod.SupplierTransfer, payable.PaymentInstrument.Method);
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
                PayableMother.TEMPLATE_PAYABLE_CREATED with
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = FIXED_NOW,
                    PayableId = payableId,
                    Description = "Aluguel sede março",
                },
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
                PayableMother.TEMPLATE_PAYABLE_CREATED with
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = FIXED_NOW,
                    PayableId = payableId,
                    Description = "Aluguel sede março",
                },
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

    public class WhenFlaggingInstrumentOutdated
    {
        // FlagInstrumentOutdated em estado limpo seta o flag, registra timestamp/motivo e emite PayableInstrumentOutdated.
        [Fact]
        public void Flag_FromCleanState_ShouldSetFlagAndEmitEvent()
        {
            var payable = PayableMother.Draft();
            payable.PullChanges();
            var when = FIXED_NOW.AddDays(5);

            payable.FlagInstrumentOutdated("Snapshot diverge em LegalName", when);

            Assert.True(payable.IsInstrumentOutdated);
            Assert.Equal(when, payable.OutdatedAt);
            Assert.Equal("Snapshot diverge em LegalName", payable.OutdatedReason);
            var emitted = Assert.IsType<PayableInstrumentOutdated>(payable.Changes.Single());
            Assert.Equal("Snapshot diverge em LegalName", emitted.Reason);
        }

        // Chamar FlagInstrumentOutdated 2x é idempotente — segunda chamada não emite nem altera state.
        [Fact]
        public void Flag_TwiceInARow_ShouldEmitOnlyOnce()
        {
            var payable = PayableMother.Draft();
            payable.FlagInstrumentOutdated("Primeiro motivo", FIXED_NOW.AddDays(5));
            payable.PullChanges();

            payable.FlagInstrumentOutdated("Segundo motivo diferente", FIXED_NOW.AddDays(10));

            Assert.Empty(payable.Changes);
            Assert.Equal("Primeiro motivo", payable.OutdatedReason); // o primeiro motivo persiste
        }

        // Motivo vazio ou só com espaços lança AP.PAY22.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Flag_WithEmptyReason_ShouldThrow_PAY22(string reason)
        {
            var payable = PayableMother.Draft();

            var ex = Assert.Throws<DomainException>(
                () => payable.FlagInstrumentOutdated(reason, FIXED_NOW.AddDays(5)));

            Assert.Equal("AP.PAY22", ex.Id);
        }

        // FlagInstrumentOutdated não muda Status (não interfere na máquina de estados).
        [Fact]
        public void Flag_ShouldNotChangeStatus()
        {
            var payable = PayableMother.Scheduled();
            var statusBefore = payable.Status;

            payable.FlagInstrumentOutdated("Motivo qualquer", FIXED_NOW.AddDays(5));

            Assert.Equal(statusBefore, payable.Status);
        }

        // Reidratação preserva IsInstrumentOutdated/OutdatedAt/OutdatedReason.
        [Fact]
        public void Rehydrate_FromStreamWithOutdatedEvent_ShouldRebuildOutdatedState()
        {
            var payable = PayableMother.Draft();
            payable.FlagInstrumentOutdated("Conta desativada", FIXED_NOW.AddDays(3));
            var stream = payable.PullChanges();

            var rehydrated = Payable.Rehydrate(stream);

            Assert.True(rehydrated.IsInstrumentOutdated);
            Assert.Equal(FIXED_NOW.AddDays(3), rehydrated.OutdatedAt);
            Assert.Equal("Conta desativada", rehydrated.OutdatedReason);
        }
    }
}
