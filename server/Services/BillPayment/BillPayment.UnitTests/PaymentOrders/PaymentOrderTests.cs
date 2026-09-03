namespace BillPayment.UnitTests.PaymentOrders;

using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.PaymentOrders.Mothers;

/// <summary>
/// A fonte de verdade da execução financeira (ADR-002): nascimento, retenções do ADR-017,
/// fila de submissão e o reflexo monotônico do que o provedor diz.
/// </summary>
public class PaymentOrderTests
{
    private static readonly DateTime Now = PaymentOrderMother.DefaultOccurredAt;
    private static readonly UserId Confirmer = UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000a"));
    private static readonly DateTimeOffset SyncedAt = new(2026, 9, 1, 13, 0, 0, TimeSpan.Zero);

    // A ordem nasce em rascunho, sem retenção, com a referência de idempotência derivada do id
    // — e sem evento: o fato de negócio foi a aprovação, o próximo é a aceitação do provedor.
    [Fact]
    public void Draft_ShouldStartClaimableWithExternalReferenceDerivedFromId()
    {
        var order = PaymentOrderMother.Draft();

        Assert.Equal(PaymentOrderStatus.Draft, order.Status);
        Assert.Equal(PaymentOrderHold.None, order.Hold);
        Assert.Equal(order.Id.Value.ToString(), order.ExternalReference);
        Assert.Equal(0, order.SubmissionAttempts);
        Assert.Empty(order.PullDomainEvents());
    }

    // Trilho nulo é defeito de composição e é recusado no nascimento.
    [Fact]
    public void Draft_WithoutARail_ShouldThrow_BLP_PMO02()
    {
        var ex = Assert.Throws<DomainException>(() => PaymentOrder.Draft(
            PaymentOrderMother.DefaultTenant,
            PaymentOrderMother.DefaultBill,
            rail: null!,
            PaymentOrderMother.DefaultScheduleFor,
            null,
            Now));

        Assert.Equal("BLP.PMO02", ex.Id);
    }

    // Tenant sem conta de pagamento retém a ordem em estado visível — não é erro nem fila girando.
    [Fact]
    public void HoldForMissingAccount_OnADraft_ShouldParkTheOrder()
    {
        var order = PaymentOrderMother.Draft();

        order.HoldForMissingAccount(Now);

        Assert.Equal(PaymentOrderHold.AwaitingAccount, order.Hold);
        Assert.Empty(order.PullDomainEvents());
    }

    // Vincular a chave devolve a ordem à fila — e limpa o aluguel, senão ela esperaria o backoff.
    [Fact]
    public void ReleaseAccountHold_ShouldReturnTheOrderToTheQueue()
    {
        var order = PaymentOrderMother.Draft();
        order.HoldForMissingAccount(Now);

        order.ReleaseAccountHold(Now);

        Assert.Equal(PaymentOrderHold.None, order.Hold);
        Assert.Null(order.SubmissionLeaseExpiresAt);
    }

    // Liberar retenção de conta que não existe é conflito de estado, não no-op silencioso.
    [Fact]
    public void ReleaseAccountHold_WhenNotHeldForAccount_ShouldThrow_BLP_PMO11()
    {
        var order = PaymentOrderMother.Draft();

        var ex = Assert.Throws<DomainException>(() => order.ReleaseAccountHold(Now));

        Assert.Equal("BLP.PMO11", ex.Id);
    }

    // A guarda da corrida cancelar×submeter: com o aluguel de submissão vigente, cancelar o
    // rascunho é recusado (BLP.PMO22) — um worker pode estar falando com o provedor NESTE
    // instante, e vencer a corrida no banco deixaria o pagamento vivo lá com o espelho
    // dizendo "cancelado".
    [Fact]
    public void CancelDraft_WhileTheSubmissionLeaseIsActive_ShouldThrow_BLP_PMO22()
    {
        var order = PaymentOrderMother.Draft();
        order.RecordSubmissionFailure(
            permanent: false, "timeout", maxAttempts: 5, TimeSpan.FromSeconds(30), Now);

        var ex = Assert.Throws<DomainException>(() => order.CancelDraft(Now.AddSeconds(10)));

        Assert.Equal("BLP.PMO22", ex.Id);
        Assert.Equal(PaymentOrderStatus.Draft, order.Status);
    }

    // Contraprova: o aluguel vence sozinho, a janela fecha, e o cancelamento local volta a valer.
    [Fact]
    public void CancelDraft_AfterTheLeaseExpires_ShouldCancelLocally()
    {
        var order = PaymentOrderMother.Draft();
        order.RecordSubmissionFailure(
            permanent: false, "timeout", maxAttempts: 5, TimeSpan.FromSeconds(30), Now);

        order.CancelDraft(Now.AddSeconds(31));

        Assert.Equal(PaymentOrderStatus.Cancelled, order.Status);
    }

    // A retenção por vencido emite o evento que vira aviso — retenção silenciosa é o modo de
    // falha do ADR-014 — e reter de novo NÃO repete o aviso.
    [Fact]
    public void HoldForConfirmation_ShouldEmitTheHeldEventExactlyOnce()
    {
        var order = PaymentOrderMother.Draft();

        order.HoldForConfirmation(Now);
        order.HoldForConfirmation(Now);

        var held = Assert.IsType<PaymentOrderHeldForConfirmationDomainEvent>(
            Assert.Single(order.PullDomainEvents()));
        Assert.Equal(order.Id, held.PaymentOrderId);
        Assert.Equal(PaymentOrderMother.DefaultBill, held.BillId);
        Assert.Equal(PaymentOrderMother.DefaultTenant, held.TenantId);
    }

    // Retenção só existe em rascunho: depois da submissão a ordem já é do provedor.
    [Fact]
    public void HoldForConfirmation_AfterSubmission_ShouldThrow_BLP_PMO13()
    {
        var order = PaymentOrderMother.Submitted();

        var ex = Assert.Throws<DomainException>(() => order.HoldForConfirmation(Now));

        Assert.Equal("BLP.PMO13", ex.Id);
    }

    // A confirmação grava quem decidiu (ADR-007/ADR-017) e devolve a ordem à fila.
    [Fact]
    public void ConfirmImmediateExecution_ShouldRecordTheAuthorAndReleaseTheOrder()
    {
        var order = PaymentOrderMother.Draft();
        order.HoldForConfirmation(Now);
        order.PullDomainEvents();

        order.ConfirmImmediateExecution(Confirmer, Now);

        Assert.Equal(PaymentOrderHold.None, order.Hold);
        Assert.Equal(Confirmer, order.ConfirmedBy);
        Assert.Equal(Now, order.ConfirmedAt);
        Assert.True(order.HasImmediateExecutionConsent);
        Assert.Null(order.SubmissionLeaseExpiresAt);
    }

    // Confirmar o que não espera confirmação é conflito — o botão apareceu para a ordem errada.
    [Fact]
    public void ConfirmImmediateExecution_WhenNothingIsPending_ShouldThrow_BLP_PMO06()
    {
        var order = PaymentOrderMother.Draft();

        var ex = Assert.Throws<DomainException>(() => order.ConfirmImmediateExecution(Confirmer, Now));

        Assert.Equal("BLP.PMO06", ex.Id);
    }

    // Pagar vencido na hora exige identidade — consentimento sem autor não é trilha.
    [Fact]
    public void ConfirmImmediateExecution_WithAnEmptyUser_ShouldThrow_BLP_PMO07()
    {
        var order = PaymentOrderMother.Draft();
        order.HoldForConfirmation(Now);

        var ex = Assert.Throws<DomainException>(
            () => order.ConfirmImmediateExecution(UserId.From(Guid.Empty), Now));

        Assert.Equal("BLP.PMO07", ex.Id);
    }

    // O aceite dado na aprovação viaja na ordem sem precisar de retenção — é o que impede a
    // fila de perguntar de novo o que a pessoa acabou de responder na tela.
    [Fact]
    public void RecordImmediateExecutionConsent_WithoutAHold_ShouldRecordTheAuthor()
    {
        var order = PaymentOrderMother.Draft();

        order.RecordImmediateExecutionConsent(Confirmer, Now);

        Assert.True(order.HasImmediateExecutionConsent);
        Assert.Equal(PaymentOrderHold.None, order.Hold);
    }

    // Submissão aceita: Pending, dados do provedor gravados, e o evento que leva o Bill a
    // Scheduled — com a data efetiva no payload.
    [Fact]
    public void MarkSubmitted_ShouldMoveToPendingAndEmitScheduled()
    {
        var order = PaymentOrderMother.Draft();
        var effective = new DateOnly(2026, 9, 11);

        order.MarkSubmitted("pay_123", effective, PaymentOrderMother.Brl(150.00m), PaymentOrderMother.Brl(1.99m), Now);

        Assert.Equal(PaymentOrderStatus.Pending, order.Status);
        Assert.Equal("pay_123", order.ProviderOrderId);
        Assert.Equal(effective, order.EffectiveScheduleDate);
        Assert.Equal(1.99m, order.Fee!.Amount);
        Assert.Null(order.SubmissionLeaseExpiresAt);

        var scheduled = Assert.IsType<PaymentOrderScheduledDomainEvent>(Assert.Single(order.PullDomainEvents()));
        Assert.Equal(effective, scheduled.EffectiveScheduleDate);
        Assert.Equal(PaymentOrderMother.DefaultBill, scheduled.BillId);
    }

    // Submeter duas vezes é defeito da fila — a segunda é recusada, não duplicada.
    [Fact]
    public void MarkSubmitted_Twice_ShouldThrow_BLP_PMO05()
    {
        var order = PaymentOrderMother.Submitted();

        var ex = Assert.Throws<DomainException>(
            () => order.MarkSubmitted("pay_456", PaymentOrderMother.DefaultScheduleFor, null, null, Now));

        Assert.Equal("BLP.PMO05", ex.Id);
    }

    // Aceitação sem id do provedor não existe — sem ele não há como conciliar nem cancelar.
    [Fact]
    public void MarkSubmitted_WithoutTheProviderId_ShouldThrow_BLP_PMO08()
    {
        var order = PaymentOrderMother.Draft();

        var ex = Assert.Throws<DomainException>(
            () => order.MarkSubmitted(" ", PaymentOrderMother.DefaultScheduleFor, null, null, Now));

        Assert.Equal("BLP.PMO08", ex.Id);
    }

    // Falha passageira empurra o aluguel (que também é o backoff) e a ordem continua em rascunho.
    [Fact]
    public void RecordSubmissionFailure_WhenTransient_ShouldPushTheLeaseAndStayDraft()
    {
        var order = PaymentOrderMother.Draft();

        var gaveUp = order.RecordSubmissionFailure(
            permanent: false, "timeout", maxAttempts: 3, TimeSpan.FromSeconds(30), Now);

        Assert.False(gaveUp);
        Assert.Equal(PaymentOrderStatus.Draft, order.Status);
        Assert.Equal("timeout", order.LastError);
        Assert.Equal(Now.AddSeconds(30), order.SubmissionLeaseExpiresAt);
        Assert.Empty(order.PullDomainEvents());
    }

    // Recusa permanente desiste na hora, guarda o motivo e emite o evento de falha — desistir
    // em silêncio é o modo de falha que este BC não tolera.
    [Fact]
    public void RecordSubmissionFailure_WhenPermanent_ShouldFailVisiblyAndEmitFailed()
    {
        var order = PaymentOrderMother.Draft();

        var gaveUp = order.RecordSubmissionFailure(
            permanent: true, "invalid_bank_slip", maxAttempts: 3, TimeSpan.FromSeconds(30), Now);

        Assert.True(gaveUp);
        Assert.Equal(PaymentOrderStatus.Failed, order.Status);
        Assert.Contains("invalid_bank_slip", order.FailReasons);

        var failed = Assert.IsType<PaymentOrderFailedDomainEvent>(Assert.Single(order.PullDomainEvents()));
        Assert.Equal(PaymentOrderMother.DefaultBill, failed.BillId);
    }

    // A espera entre tentativas tem teto de 30 minutos: mesmo uma base desproporcional não
    // empurra o aluguel além dele — é o mesmo teto das outras filas do BC.
    [Fact]
    public void RecordSubmissionFailure_WithAnOversizedBaseDelay_ShouldCapTheLeaseAtThirtyMinutes()
    {
        var order = PaymentOrderMother.Draft();

        var gaveUp = order.RecordSubmissionFailure(
            permanent: false, "timeout", maxAttempts: 3, TimeSpan.FromHours(2), Now);

        Assert.False(gaveUp);
        Assert.Equal(Now.AddMinutes(30), order.SubmissionLeaseExpiresAt);
    }

    // Falha passageira com o orçamento de tentativas esgotado desiste como a permanente:
    // vira Failed visível com o evento — a fila não gira em silêncio além do teto.
    [Fact]
    public void RecordSubmissionFailure_WhenTransientButAttemptsAreExhausted_ShouldGiveUpVisibly()
    {
        var order = PaymentOrderMother.Draft();

        var gaveUp = order.RecordSubmissionFailure(
            permanent: false, "timeout", maxAttempts: 0, TimeSpan.FromSeconds(30), Now);

        Assert.True(gaveUp);
        Assert.Equal(PaymentOrderStatus.Failed, order.Status);
        Assert.Contains("timeout", order.FailReasons);
        Assert.IsType<PaymentOrderFailedDomainEvent>(Assert.Single(order.PullDomainEvents()));
    }

    // Id do provedor é referência, não diagnóstico: acima de 100 caracteres a submissão é
    // RECUSADA (PMO23) — truncar produziria uma chave que consulta o vazio para sempre.
    [Fact]
    public void MarkSubmitted_WithAnOversizedProviderOrderId_ShouldThrow_BLP_PMO23()
    {
        var order = PaymentOrderMother.Draft();
        var oversized = new string('p', PaymentOrder.PROVIDER_ORDER_ID_MAX_LENGTH + 50);

        var ex = Assert.Throws<DomainException>(
            () => order.MarkSubmitted(oversized, PaymentOrderMother.DefaultScheduleFor, null, null, Now));

        Assert.Equal("BLP.PMO23", ex.Id);
        Assert.Equal(PaymentOrderStatus.Draft, order.Status);
    }

    // Comportamento ATUAL documentado: erro da fila acima de 500 caracteres é truncado em
    // silêncio, tanto no diagnóstico (LastError) quanto no motivo acumulado da desistência.
    [Fact]
    public void RecordSubmissionFailure_WithAnOversizedError_ShouldClampDiagnosticsAndReason()
    {
        var order = PaymentOrderMother.Draft();
        var oversized = new string('e', PaymentOrder.LAST_ERROR_MAX_LENGTH + 100);

        order.RecordSubmissionFailure(
            permanent: true, oversized, maxAttempts: 3, TimeSpan.FromSeconds(30), Now);
        order.PullDomainEvents();

        Assert.Equal(oversized[..PaymentOrder.LAST_ERROR_MAX_LENGTH], order.LastError);
        Assert.Equal(oversized[..PaymentOrder.FAIL_REASON_MAX_LENGTH], Assert.Single(order.FailReasons));
    }

    // Comportamento ATUAL documentado: motivo de falha vindo do provedor acima de 500
    // caracteres também é truncado em silêncio — o mesmo Clamp dos demais campos longos.
    [Fact]
    public void ApplyProviderStatus_WithAnOversizedFailReason_ShouldClampTheReason()
    {
        var order = PaymentOrderMother.Submitted();
        var oversized = new string('r', PaymentOrder.FAIL_REASON_MAX_LENGTH + 100);

        order.ApplyProviderStatus(PaymentOrderStatus.Failed, null, fee: null, [oversized], SyncedAt, Now);
        order.PullDomainEvents();

        Assert.Equal(oversized[..PaymentOrder.FAIL_REASON_MAX_LENGTH], Assert.Single(order.FailReasons));
    }

    // Comportamento ATUAL documentado: chave de comprovante acima de 200 caracteres é truncada
    // em silêncio — truncar chave de balde muda o objeto apontado, e este teste fixa o fato.
    [Fact]
    public void AttachReceipt_WithAnOversizedStorageKey_ShouldClampTheKey()
    {
        var order = PaymentOrderMother.Submitted();
        order.ApplyProviderStatus(PaymentOrderStatus.Paid, new DateOnly(2026, 9, 11), fee: null, null, SyncedAt, Now);
        order.PullDomainEvents();
        var oversized = new string('k', PaymentOrder.RECEIPT_STORAGE_KEY_MAX_LENGTH + 40);

        order.AttachReceipt(oversized, Now);

        Assert.Equal(oversized[..PaymentOrder.RECEIPT_STORAGE_KEY_MAX_LENGTH], order.ReceiptStorageKey);
    }

    // A defesa em profundidade da submissão: sem valor resolvido, nada vai ao gateway.
    [Fact]
    public void EnsureSubmittable_WithoutAnyResolvedAmount_ShouldThrow_BLP_PMO10()
    {
        var order = PaymentOrderMother.DraftWithoutAmount();

        var ex = Assert.Throws<DomainException>(() => order.EnsureSubmittable(null));

        Assert.Equal("BLP.PMO10", ex.Id);
    }

    [Fact]
    public void EnsureSubmittable_WithAResolvedAmount_ShouldPass()
    {
        var order = PaymentOrderMother.DraftWithoutAmount();

        order.EnsureSubmittable(PaymentOrderMother.Brl(615.07m));
    }

    // A marca definitiva de "sem comprovante" tira a ordem paga da varredura da rede de segurança.
    [Fact]
    public void MarkReceiptMissing_OnAPaidOrder_ShouldRecordTheDefinitiveOutcome()
    {
        var order = PaymentOrderMother.Submitted();
        order.ApplyProviderStatus(PaymentOrderStatus.Paid, new DateOnly(2026, 9, 11), fee: null, null, SyncedAt, Now);
        order.PullDomainEvents();

        order.MarkReceiptMissing(Now);

        Assert.True(order.ReceiptUnavailable);
    }

    [Fact]
    public void MarkReceiptMissing_BeforePayment_ShouldThrow_BLP_PMO15()
    {
        var order = PaymentOrderMother.Submitted();

        var ex = Assert.Throws<DomainException>(() => order.MarkReceiptMissing(Now));

        Assert.Equal("BLP.PMO15", ex.Id);
    }

    // Com arquivo no balde a marca seria mentira — o método ignora em vez de sobrescrever.
    [Fact]
    public void MarkReceiptMissing_WhenAReceiptIsAlreadyStored_ShouldBeIgnored()
    {
        var order = PaymentOrderMother.Submitted();
        order.ApplyProviderStatus(PaymentOrderStatus.Paid, new DateOnly(2026, 9, 11), fee: null, null, SyncedAt, Now);
        order.PullDomainEvents();
        order.AttachReceipt("tenants/x/comprovante.pdf", Now);

        order.MarkReceiptMissing(Now);

        Assert.False(order.ReceiptUnavailable);
    }

    // O arquivo chegando depois da marca vence a marca — o desfecho melhorou.
    [Fact]
    public void AttachReceipt_AfterTheMissingMark_ShouldClearIt()
    {
        var order = PaymentOrderMother.Submitted();
        order.ApplyProviderStatus(PaymentOrderStatus.Paid, new DateOnly(2026, 9, 11), fee: null, null, SyncedAt, Now);
        order.PullDomainEvents();
        order.MarkReceiptMissing(Now);

        order.AttachReceipt("tenants/x/comprovante.pdf", Now);

        Assert.False(order.ReceiptUnavailable);
        Assert.Equal("tenants/x/comprovante.pdf", order.ReceiptStorageKey);
    }

    // Registrar falha sem o erro que a causou é diagnóstico perdido — recusado.
    [Fact]
    public void RecordSubmissionFailure_WithABlankError_ShouldThrow_BLP_PMO12()
    {
        var order = PaymentOrderMother.Draft();

        var ex = Assert.Throws<DomainException>(() => order.RecordSubmissionFailure(
            permanent: false, " ", maxAttempts: 3, TimeSpan.FromSeconds(30), Now));

        Assert.Equal("BLP.PMO12", ex.Id);
    }

    // O reflexo do provedor é monotônico: Paid não regride — o webhook atrasado de
    // BankProcessing é ignorado, e a sincronização ainda assim fica carimbada.
    [Fact]
    public void ApplyProviderStatus_OutOfOrder_ShouldBeIgnoredAndStillStampTheSync()
    {
        var order = PaymentOrderMother.Submitted();
        order.ApplyProviderStatus(PaymentOrderStatus.Paid, new DateOnly(2026, 9, 11), fee: null, null, SyncedAt, Now);
        order.PullDomainEvents();

        var applied = order.ApplyProviderStatus(
            PaymentOrderStatus.BankProcessing, null, fee: null, null, SyncedAt.AddMinutes(5), Now);

        Assert.False(applied);
        Assert.Equal(PaymentOrderStatus.Paid, order.Status);
        Assert.Equal(SyncedAt.AddMinutes(5), order.LastProviderSyncAt);
        Assert.Empty(order.PullDomainEvents());
    }

    // Pago sem data de pagamento é retrato incoerente — lança em vez de gravar mentira.
    [Fact]
    public void ApplyProviderStatus_PaidWithoutAPaymentDate_ShouldThrow_BLP_PMO03()
    {
        var order = PaymentOrderMother.Submitted();

        var ex = Assert.Throws<DomainException>(() => order.ApplyProviderStatus(
            PaymentOrderStatus.Paid, paidAt: null, fee: null, null, SyncedAt, Now));

        Assert.Equal("BLP.PMO03", ex.Id);
    }

    // O caminho feliz do webhook: Paid grava a data, a taxa e emite o evento com o payload certo.
    [Fact]
    public void ApplyProviderStatus_Paid_ShouldEmitPaidWithTheDate()
    {
        var order = PaymentOrderMother.Submitted();
        var paidAt = new DateOnly(2026, 9, 11);

        var applied = order.ApplyProviderStatus(
            PaymentOrderStatus.Paid, paidAt, PaymentOrderMother.Brl(2.49m), null, SyncedAt, Now);

        Assert.True(applied);
        Assert.Equal(paidAt, order.PaidAt);
        Assert.Equal(2.49m, order.Fee!.Amount);

        var paid = Assert.IsType<PaymentOrderPaidDomainEvent>(Assert.Single(order.PullDomainEvents()));
        Assert.Equal(paidAt, paid.PaidAt);
    }

    // Uma ordem que nós ainda não submetemos não tem estado no provedor — retrato que chegue
    // em Draft é de outra ordem ou de outro tempo, e é ignorado.
    [Fact]
    public void ApplyProviderStatus_OnADraft_ShouldBeIgnored()
    {
        var order = PaymentOrderMother.Draft();

        var applied = order.ApplyProviderStatus(
            PaymentOrderStatus.Pending, null, fee: null, null, SyncedAt, Now);

        Assert.False(applied);
        Assert.Equal(PaymentOrderStatus.Draft, order.Status);
    }

    // O estorno vem DEPOIS de pago, é a única saída de Paid, e emite o evento próprio.
    [Fact]
    public void ApplyProviderStatus_RefundedAfterPaid_ShouldEmitRefunded()
    {
        var order = PaymentOrderMother.Submitted();
        order.ApplyProviderStatus(PaymentOrderStatus.Paid, new DateOnly(2026, 9, 11), fee: null, null, SyncedAt, Now);
        order.PullDomainEvents();

        var applied = order.ApplyProviderStatus(
            PaymentOrderStatus.Refunded, null, fee: null, null, SyncedAt.AddDays(1), Now);

        Assert.True(applied);
        Assert.Equal(PaymentOrderStatus.Refunded, order.Status);
        Assert.IsType<PaymentOrderRefundedDomainEvent>(Assert.Single(order.PullDomainEvents()));
    }

    // Falha do provedor acumula os motivos — são eles que a fila operacional mostra.
    [Fact]
    public void ApplyProviderStatus_Failed_ShouldCollectTheReasonsAndEmitFailed()
    {
        var order = PaymentOrderMother.Submitted();

        order.ApplyProviderStatus(
            PaymentOrderStatus.Failed, null, fee: null, ["saldo insuficiente"], SyncedAt, Now);

        Assert.Contains("saldo insuficiente", order.FailReasons);
        Assert.IsType<PaymentOrderFailedDomainEvent>(Assert.Single(order.PullDomainEvents()));
    }

    // Cancelar um rascunho é local — o provedor nem sabe dela — e emite o evento de cancelamento.
    [Fact]
    public void CancelDraft_ShouldCancelLocallyAndEmitCancelled()
    {
        var order = PaymentOrderMother.Draft();

        order.CancelDraft(Now);

        Assert.Equal(PaymentOrderStatus.Cancelled, order.Status);
        Assert.IsType<PaymentOrderCancelledDomainEvent>(Assert.Single(order.PullDomainEvents()));
    }

    // Depois da submissão o cancelamento local é proibido: quem decide é o provedor, e o estado
    // local só muda quando ele confirmar.
    [Fact]
    public void CancelDraft_AfterSubmission_ShouldThrow_BLP_PMO09()
    {
        var order = PaymentOrderMother.Submitted();

        var ex = Assert.Throws<DomainException>(() => order.CancelDraft(Now));

        Assert.Equal("BLP.PMO09", ex.Id);
    }

    // O comprovante só existe de pagamento que aconteceu — anexá-lo a uma ordem pendente é defeito.
    [Fact]
    public void AttachReceipt_OnAPendingOrder_ShouldThrow_BLP_PMO15()
    {
        var order = PaymentOrderMother.Submitted();

        var ex = Assert.Throws<DomainException>(
            () => order.AttachReceipt("tenant-1/receipts/r1.pdf", Now));

        Assert.Equal("BLP.PMO15", ex.Id);
    }

    // Pago, o comprovante entra com a chave do balde — nunca a URL do provedor.
    [Fact]
    public void AttachReceipt_OnAPaidOrder_ShouldStoreTheStorageKey()
    {
        var order = PaymentOrderMother.Submitted();
        order.ApplyProviderStatus(PaymentOrderStatus.Paid, new DateOnly(2026, 9, 11), fee: null, null, SyncedAt, Now);
        order.PullDomainEvents();

        order.AttachReceipt("tenant-1/receipts/r1.pdf", Now);

        Assert.Equal("tenant-1/receipts/r1.pdf", order.ReceiptStorageKey);
    }
}
