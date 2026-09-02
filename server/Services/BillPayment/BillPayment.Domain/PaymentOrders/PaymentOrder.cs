namespace BillPayment.Domain.PaymentOrders;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// A ordem de pagamento no provedor — <strong>fonte de verdade da execução financeira</strong>
/// (ADR-002). O <c>Bill</c> cobre captura, verificação e decisão humana; daqui em diante quem
/// dita o estado é o provedor, e o <c>Bill</c> só reflete por evento.
/// </summary>
/// <remarks>
/// <para>
/// Nasce em <c>Draft</c> pelo handler de <c>BillApprovedDomainEvent</c> — <strong>sem chamada
/// externa na transação</strong> — e é submetida por um worker de fila (aluguel em coluna, teto
/// de tentativas, desistência visível), no mesmo molde da fila de leitura por IA. A chave de
/// idempotência de ponta a ponta é <see cref="ExternalReference"/>: uma retentativa após
/// timeout <strong>consulta o provedor por ela antes de reenviar</strong>.
/// </para>
/// <para>
/// <see cref="Hold"/> materializa o ADR-017: ordem retida é estado visível, nunca fila que gira
/// em silêncio — sem conta de pagamento espera a chave, execução imediata espera gente confirmar.
/// </para>
/// </remarks>
public sealed class PaymentOrder : AggregateRoot<PaymentOrderId>
{
    public const int PROVIDER_ORDER_ID_MAX_LENGTH = 100;
    public const int LAST_ERROR_MAX_LENGTH = 500;
    public const int FAIL_REASON_MAX_LENGTH = 500;
    public const int RECEIPT_STORAGE_KEY_MAX_LENGTH = 200;

    /// <summary>Teto da espera entre tentativas de submissão. A espera dobra e para aqui.</summary>
    private static readonly TimeSpan MAX_SUBMISSION_RETRY_DELAY = TimeSpan.FromMinutes(30);

    /// <summary>Quantas duplicações de espera antes de o expoente parar de crescer.</summary>
    private const int MAX_SUBMISSION_BACKOFF_SHIFT = 10;

    private readonly List<string> _failReasons = [];

    public TenantId TenantId { get; private set; }

    /// <summary>O boleto que esta ordem paga. Referência por id — nunca navegação (ADR-002).</summary>
    public BillId BillId { get; private set; }

    /// <summary>Trilho herdado do <c>Bill</c> no nascimento (ADR-010). Decide qual gateway paga.</summary>
    public PaymentRail Rail { get; private set; } = default!;

    public PaymentOrderStatus Status { get; private set; } = default!;

    /// <summary>Por que a ordem está fora da fila de submissão. <c>None</c> = elegível.</summary>
    public PaymentOrderHold Hold { get; private set; } = default!;

    /// <summary>A data que o aprovador pediu. A efetiva sai do agendamento e pode diferir.</summary>
    public DateOnly RequestedScheduleDate { get; private set; }

    /// <summary>A data que o agendamento calculou e o provedor aceitou. Nula até a submissão.</summary>
    public DateOnly? EffectiveScheduleDate { get; private set; }

    /// <summary>Id da ordem no provedor. Nulo até a submissão ser aceita.</summary>
    public string? ProviderOrderId { get; private set; }

    /// <summary>Valor submetido (ou a submeter). Nulo quando nenhuma fonte o forneceu ainda.</summary>
    public Money? Amount { get; private set; }

    /// <summary>Taxa do provedor, quando informada. Entra no relatório como linha própria.</summary>
    public Money? Fee { get; private set; }

    public DateOnly? PaidAt { get; private set; }

    /// <summary>Motivos de falha, acumulados — do provedor ou da desistência da submissão.</summary>
    public IReadOnlyCollection<string> FailReasons => _failReasons.AsReadOnly();

    /// <summary>Instante da última sincronização com o provedor — webhook ou conciliação.</summary>
    public DateTimeOffset? LastProviderSyncAt { get; private set; }

    /// <summary>
    /// Chave do comprovante no balde, cifrado e prefixado por tenant. Nula até a captura da 3.3
    /// baixar o arquivo — <strong>o arquivo é a evidência, não a URL do provedor</strong>.
    /// </summary>
    public string? ReceiptStorageKey { get; private set; }

    /// <summary>Quantas vezes a fila de submissão já pegou esta ordem.</summary>
    public int SubmissionAttempts { get; private set; }

    /// <summary>
    /// Até quando a submissão é de um worker — e, depois de uma falha passageira, a partir de
    /// quando vale tentar de novo. Uma coluna só: é a mesma pergunta.
    /// </summary>
    public DateTime? SubmissionLeaseExpiresAt { get; private set; }

    /// <summary>O último erro visto pela fila. Diagnóstico, nunca decisão.</summary>
    public string? LastError { get; private set; }

    /// <summary>Quem confirmou a execução imediata (ADR-017). Nulo quando nunca foi preciso.</summary>
    public UserId? ConfirmedBy { get; private set; }

    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>
    /// A chave de idempotência enviada ao provedor — o próprio id da ordem. Derivada, para não
    /// existir caminho em que as duas divirjam.
    /// </summary>
    public string ExternalReference => Id.Value.ToString();

    private PaymentOrder() { }

    private PaymentOrder(PaymentOrderId id) : base(id) { }

    /// <summary>
    /// Nasce a ordem, em <c>Draft</c>, elegível para a fila de submissão. Não emite evento:
    /// o fato de negócio foi a aprovação, e o próximo fato é a aceitação pelo provedor.
    /// </summary>
    public static PaymentOrder Draft(
        TenantId tenantId,
        BillId billId,
        PaymentRail rail,
        DateOnly requestedScheduleDate,
        Money? amount,
        DateTime occurredAt)
    {
        if (rail is null)
            throw PaymentOrderErrors.RailRequired();

        var order = new PaymentOrder(PaymentOrderId.New())
        {
            TenantId = tenantId,
            BillId = billId,
            Rail = rail,
            Status = PaymentOrderStatus.Draft,
            Hold = PaymentOrderHold.None,
            RequestedScheduleDate = requestedScheduleDate,
            Amount = amount,
        };

        order.CreatedAt = occurredAt;
        order.UpdatedAt = occurredAt;

        return order;
    }

    /// <summary>Retém: o tenant não tem conta de pagamento. A varredura reconfere e destrava.</summary>
    public void HoldForMissingAccount(DateTime occurredAt)
    {
        if (Status != PaymentOrderStatus.Draft)
            throw PaymentOrderErrors.HoldRequiresDraft(Status.Name);

        Hold = PaymentOrderHold.AwaitingAccount;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Retém: a execução seria imediata (boleto vencido) e o ADR-017 exige gente confirmando.
    /// Emite o evento que leva o aviso ao tenant — retenção silenciosa é o modo de falha
    /// que o ADR-014 existe para impedir.
    /// </summary>
    public void HoldForConfirmation(DateTime occurredAt)
    {
        if (Status != PaymentOrderStatus.Draft)
            throw PaymentOrderErrors.HoldRequiresDraft(Status.Name);

        var alreadyHeld = Hold == PaymentOrderHold.AwaitingConfirmation;
        Hold = PaymentOrderHold.AwaitingConfirmation;
        UpdatedAt = occurredAt;

        if (!alreadyHeld)
            AddDomainEvent(new PaymentOrderHeldForConfirmationDomainEvent(Id, TenantId, BillId, occurredAt));
    }

    /// <summary>O tenant vinculou a chave — a ordem volta para a fila.</summary>
    public void ReleaseAccountHold(DateTime occurredAt)
    {
        if (Hold != PaymentOrderHold.AwaitingAccount)
            throw PaymentOrderErrors.AccountHoldNotPending();

        Hold = PaymentOrderHold.None;
        SubmissionLeaseExpiresAt = null;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Uma pessoa confirma que quer pagar AGORA um boleto que o provedor processa na hora.
    /// A autoria fica gravada — é a trilha do ADR-017, no molde do aceite de risco.
    /// </summary>
    public void ConfirmImmediateExecution(UserId confirmedBy, DateTime occurredAt)
    {
        if (Hold != PaymentOrderHold.AwaitingConfirmation)
            throw PaymentOrderErrors.ConfirmationNotPending();

        RecordImmediateExecutionConsent(confirmedBy, occurredAt);
        Hold = PaymentOrderHold.None;
        SubmissionLeaseExpiresAt = null;
    }

    /// <summary>
    /// Grava o consentimento SEM exigir a retenção — é o caminho do aceite dado já na
    /// aprovação (o boleto estava vencido na tela, e o aprovador confirmou lá).
    /// </summary>
    public void RecordImmediateExecutionConsent(UserId confirmedBy, DateTime occurredAt)
    {
        if (confirmedBy.Value == Guid.Empty)
            throw PaymentOrderErrors.ConfirmationRequiresUser();

        ConfirmedBy = confirmedBy;
        ConfirmedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    /// <summary>A execução imediata já foi confirmada por alguém — não se pergunta duas vezes.</summary>
    public bool HasImmediateExecutionConsent => ConfirmedBy is not null;

    /// <summary>
    /// O provedor aceitou a ordem. <c>Draft → Pending</c>, e o evento leva o <c>Bill</c> a
    /// <c>Scheduled</c>.
    /// </summary>
    public void MarkSubmitted(
        string providerOrderId,
        DateOnly effectiveScheduleDate,
        Money? amount,
        Money? fee,
        DateTime occurredAt)
    {
        if (Status != PaymentOrderStatus.Draft)
            throw PaymentOrderErrors.SubmissionRequiresDraft(Status.Name);
        if (string.IsNullOrWhiteSpace(providerOrderId))
            throw PaymentOrderErrors.ProviderOrderIdRequired();

        ProviderOrderId = Clamp(providerOrderId, PROVIDER_ORDER_ID_MAX_LENGTH);
        EffectiveScheduleDate = effectiveScheduleDate;
        Amount = amount ?? Amount;
        Fee = fee ?? Fee;
        Status = PaymentOrderStatus.Pending;
        SubmissionLeaseExpiresAt = null;
        LastError = null;
        UpdatedAt = occurredAt;

        AddDomainEvent(new PaymentOrderScheduledDomainEvent(
            Id, TenantId, BillId, effectiveScheduleDate, occurredAt));
    }

    /// <summary>
    /// A submissão falhou. Devolve <c>true</c> quando a ordem desistiu de vez — e aí emite o
    /// evento de falha, porque desistir em silêncio é o modo de falha que este BC não tolera.
    /// </summary>
    /// <remarks>
    /// Permanente desiste na hora; passageira ganha as tentativas com espera que dobra. Errar
    /// para "passageira" é o lado seguro <strong>aqui também</strong>, porque toda retentativa
    /// começa conferindo por <see cref="ExternalReference"/> — reenvio cego não existe.
    /// </remarks>
    public bool RecordSubmissionFailure(
        bool permanent,
        string error,
        int maxAttempts,
        TimeSpan baseRetryDelay,
        DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw PaymentOrderErrors.SubmissionErrorRequired();
        if (Status != PaymentOrderStatus.Draft)
            throw PaymentOrderErrors.SubmissionRequiresDraft(Status.Name);

        LastError = Clamp(error, LAST_ERROR_MAX_LENGTH);

        if (!permanent && SubmissionAttempts < maxAttempts)
        {
            SubmissionLeaseExpiresAt = NextSubmissionAttemptAt(baseRetryDelay, occurredAt);
            UpdatedAt = occurredAt;
            return false;
        }

        Status = PaymentOrderStatus.Failed;
        _failReasons.Add(Clamp(error, FAIL_REASON_MAX_LENGTH));
        SubmissionLeaseExpiresAt = null;
        UpdatedAt = occurredAt;

        AddDomainEvent(new PaymentOrderFailedDomainEvent(Id, TenantId, BillId, occurredAt));
        return true;
    }

    /// <summary>
    /// Reflete o estado que o provedor conhece — webhook ou conciliação. <strong>Idempotente e
    /// monotônica</strong>: fora de ordem é ignorado (devolve <c>false</c>), nunca lançado.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Draft</c> ignora tudo: uma ordem que nós ainda não submetemos não tem estado no
    /// provedor — um retrato que chegue aqui é de outra ordem ou de outro tempo.
    /// </para>
    /// <para>
    /// Incoerência lança (<c>BLP.PMO03</c>): pago sem data de pagamento não é atraso de rede,
    /// é mentira — e gravá-la contaminaria a trilha que o comprovante referencia.
    /// </para>
    /// </remarks>
    public bool ApplyProviderStatus(
        PaymentOrderStatus target,
        DateOnly? paidAt,
        Money? fee,
        IReadOnlyCollection<string>? failReasons,
        DateTimeOffset syncedAt,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(target);

        LastProviderSyncAt = syncedAt;
        UpdatedAt = occurredAt;

        if (Status == PaymentOrderStatus.Draft || target == Status || !Status.CanTransitionTo(target))
            return false;

        if (target == PaymentOrderStatus.Paid && paidAt is null)
            throw PaymentOrderErrors.IncoherentProviderPayload("pago sem data de pagamento");

        Status = target;
        Fee = fee ?? Fee;

        _failReasons.AddRange((failReasons ?? [])
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Select(reason => Clamp(reason, FAIL_REASON_MAX_LENGTH)));

        if (target == PaymentOrderStatus.Paid)
        {
            PaidAt = paidAt;
            AddDomainEvent(new PaymentOrderPaidDomainEvent(Id, TenantId, BillId, paidAt!.Value, occurredAt));
        }
        else if (target == PaymentOrderStatus.Failed)
        {
            AddDomainEvent(new PaymentOrderFailedDomainEvent(Id, TenantId, BillId, occurredAt));
        }
        else if (target == PaymentOrderStatus.Cancelled)
        {
            AddDomainEvent(new PaymentOrderCancelledDomainEvent(Id, TenantId, BillId, occurredAt));
        }
        else if (target == PaymentOrderStatus.Refunded)
        {
            AddDomainEvent(new PaymentOrderRefundedDomainEvent(Id, TenantId, BillId, occurredAt));
        }

        return true;
    }

    /// <summary>
    /// Cancela uma ordem que o provedor ainda não conhece. O caminho pós-submissão é outro:
    /// pedir ao provedor e refletir por <see cref="ApplyProviderStatus"/>.
    /// </summary>
    public void CancelDraft(DateTime occurredAt)
    {
        if (Status != PaymentOrderStatus.Draft)
            throw PaymentOrderErrors.CancellationNotAllowed(Status.Name);

        Status = PaymentOrderStatus.Cancelled;
        SubmissionLeaseExpiresAt = null;
        UpdatedAt = occurredAt;

        AddDomainEvent(new PaymentOrderCancelledDomainEvent(Id, TenantId, BillId, occurredAt));
    }

    /// <summary>Guarda a chave do comprovante baixado e cifrado no balde.</summary>
    public void AttachReceipt(string storageKey, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw PaymentOrderErrors.ReceiptStorageKeyRequired();
        if (Status != PaymentOrderStatus.Paid && Status != PaymentOrderStatus.Refunded)
            throw PaymentOrderErrors.ReceiptRequiresPayment(Status.Name);

        ReceiptStorageKey = Clamp(storageKey, RECEIPT_STORAGE_KEY_MAX_LENGTH);
        UpdatedAt = occurredAt;
    }

    /// <summary>A espera dobra a cada falha, com o mesmo teto das outras filas do BC.</summary>
    private DateTime NextSubmissionAttemptAt(TimeSpan baseRetryDelay, DateTime occurredAt)
    {
        var shift = Math.Min(Math.Max(SubmissionAttempts - 1, 0), MAX_SUBMISSION_BACKOFF_SHIFT);
        var scaled = baseRetryDelay.Ticks * (1L << shift);

        return occurredAt.AddTicks(Math.Min(scaled, MAX_SUBMISSION_RETRY_DELAY.Ticks));
    }

    private static string Clamp(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
