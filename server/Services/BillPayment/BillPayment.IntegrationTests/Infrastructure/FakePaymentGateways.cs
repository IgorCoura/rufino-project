namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Os dois gateways de pagamento, determinísticos — a resposta do provedor é ENTRADA do fluxo
/// sob teste, e a tradução da resposta real é dos testes de adapter.
/// </summary>
/// <remarks>
/// Singleton com <see cref="Reset"/>, como o <see cref="FakeLookupServices"/>. O que ele grava
/// (referência, data, contadores) é o que os testes afirmam: a idempotência por
/// <c>externalReference</c> e o "não reenviou" são provados por contador, não por fé.
/// </remarks>
internal sealed class FakePaymentGateways : IBillPaymentGateway, IPixPaymentGateway
{
    /// <summary>Resposta da submissão. Nula = aceita ecoando a data pedida.</summary>
    public PaymentSubmissionResult? ScriptedSubmission { get; set; }

    public PaymentFetchResult ScriptedFind { get; set; } = PaymentFetchResult.NotFound();

    public PaymentFetchResult ScriptedGet { get; set; } = PaymentFetchResult.NotFound();

    public PaymentCancellationResult ScriptedCancel { get; set; } = PaymentCancellationResult.Cancelled();

    public int SubmissionCalls { get; private set; }

    public int FindCalls { get; private set; }

    public int CancelCalls { get; private set; }

    public string? LastExternalReference { get; private set; }

    public DateOnly? LastScheduleDate { get; private set; }

    public decimal? LastAmount { get; private set; }

    public void Reset()
    {
        ScriptedSubmission = null;
        ScriptedFind = PaymentFetchResult.NotFound();
        ScriptedGet = PaymentFetchResult.NotFound();
        ScriptedCancel = PaymentCancellationResult.Cancelled();
        SubmissionCalls = 0;
        FindCalls = 0;
        CancelCalls = 0;
        LastExternalReference = null;
        LastScheduleDate = null;
        LastAmount = null;
    }

    public Task<PaymentSubmissionResult> ScheduleAsync(
        CredentialRef? credential,
        DigitableLine digitableLine,
        Money amount,
        DateOnly? dueDate,
        DateOnly? scheduleDate,
        string externalReference,
        string? description,
        CancellationToken cancellationToken)
        => Task.FromResult(Submit(amount, scheduleDate, externalReference));

    public Task<PaymentSubmissionResult> PayAsync(
        CredentialRef? credential,
        PixPayload payload,
        Money amount,
        DateOnly? scheduleDate,
        string externalReference,
        string? description,
        CancellationToken cancellationToken)
        => Task.FromResult(Submit(amount, scheduleDate, externalReference));

    public Task<PaymentFetchResult> FindByExternalReferenceAsync(
        CredentialRef? credential,
        string externalReference,
        CancellationToken cancellationToken)
    {
        FindCalls++;
        LastExternalReference = externalReference;
        return Task.FromResult(ScriptedFind);
    }

    public Task<PaymentFetchResult> GetAsync(
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken)
        => Task.FromResult(ScriptedGet);

    public Task<PaymentCancellationResult> CancelAsync(
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken)
    {
        CancelCalls++;
        return Task.FromResult(ScriptedCancel);
    }

    private PaymentSubmissionResult Submit(Money amount, DateOnly? scheduleDate, string externalReference)
    {
        SubmissionCalls++;
        LastExternalReference = externalReference;
        LastScheduleDate = scheduleDate;
        LastAmount = amount.Amount;

        return ScriptedSubmission ?? PaymentSubmissionResult.Accepted(new ProviderPaymentSnapshot(
            "pay_fake_1",
            PaymentOrderStatus.Pending,
            "PENDING",
            scheduleDate,
            PaidAt: null,
            Fee: null,
            FailReasons: [],
            ReceiptUrl: null));
    }
}
