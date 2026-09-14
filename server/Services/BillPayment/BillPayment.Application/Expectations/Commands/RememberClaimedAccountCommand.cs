namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cumpre o "lembrar desta conta" de uma reivindicação: o número pedido passa a morar numa
/// expectativa do beneficiário, e os próximos boletos com ele chegam roteados (ADR-026).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Roda depois da verificação, não da reivindicação</strong>, pelo mesmo motivo do
/// cumprimento de ciclo: é a verificação que resolve o beneficiário, e a expectativa não existe sem
/// ele. Sem beneficiário cadastrado o pedido fica pendente no item, e a próxima verificação — depois
/// de alguém cadastrá-lo — tenta de novo.
/// </para>
/// <para>
/// <strong>Idempotente por construção, e sem apagar o pedido.</strong> O item continua registrando o
/// que a pessoa pediu; "cumprido" é existir expectativa com aquele número. Assim o comando muta um
/// agregado só, e a reentrega do outbox não duplica nada.
/// </para>
/// </remarks>
public sealed record RememberClaimedAccountCommand(Guid TenantId, Guid BillId)
    : ITenantScopedCommand, IRequest<RememberClaimedAccountResponse>;

/// <param name="Outcome">
/// <c>NothingToRemember</c>, <c>AwaitingPayee</c>, <c>AlreadyRemembered</c>, <c>FilledExisting</c> ou
/// <c>Created</c>.
/// </param>
public sealed record RememberClaimedAccountResponse(Guid BillId, string Outcome, Guid? ExpectationId);

public sealed class RememberClaimedAccountCommandHandler(
    IBillRepository bills,
    ICaptureItemRepository items,
    IPayeeRepository payees,
    IBillExpectationRepository expectations,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<RememberClaimedAccountCommandHandler> logger)
    : IRequestHandler<RememberClaimedAccountCommand, RememberClaimedAccountResponse>
{
    public const string OUTCOME_NOTHING_TO_REMEMBER = "NothingToRemember";
    public const string OUTCOME_AWAITING_PAYEE = "AwaitingPayee";
    public const string OUTCOME_ALREADY_REMEMBERED = "AlreadyRemembered";
    public const string OUTCOME_FILLED_EXISTING = "FilledExisting";
    public const string OUTCOME_CREATED = "Created";

    /// <summary>Quando o boleto não informa vencimento, o dia mais comum das contas mensais.</summary>
    private const int FALLBACK_DUE_DAY = 10;

    public async Task<RememberClaimedAccountResponse> Handle(
        RememberClaimedAccountCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var billId = BillId.From(request.BillId);

        var item = await items.FindRememberingAccountForBillAsync(tenantId, billId, cancellationToken);
        if (item?.RememberedAccountReference is not { } account)
            return new(request.BillId, OUTCOME_NOTHING_TO_REMEMBER, null);

        var bill = await bills.GetAsync(tenantId, billId, cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        if (bill.PayeeId is not { } payeeId)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Pedido de lembrar a conta pendente: o beneficiário do boleto {BillId} não está cadastrado.",
                    request.BillId);
            }

            return new(request.BillId, OUTCOME_AWAITING_PAYEE, null);
        }

        var existing = await expectations.ListByPayeeAsync(tenantId, payeeId, cancellationToken);

        if (existing.FirstOrDefault(e => AccountReferenceMatchingService.SignificantDigits(e.AccountReference) == account)
            is { } remembered)
        {
            return new(request.BillId, OUTCOME_ALREADY_REMEMBERED, remembered.Id.Value);
        }

        var now = clock.GetUtcNow().UtcDateTime;

        // A expectativa sem número que o beneficiário já tinha é ESTA conta ganhando o número: criar
        // outra ao lado dela duplicaria o alerta e deixaria o casamento de ciclos ambíguo.
        if (existing.Count == 1 && string.IsNullOrEmpty(existing.First().AccountReference))
        {
            var blank = existing.First();

            blank.AssignAccountReference(account, now);

            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return new(request.BillId, OUTCOME_FILLED_EXISTING, blank.Id.Value);
        }

        var payee = await payees.GetAsync(tenantId, payeeId, cancellationToken)
            ?? throw PayeeErrors.NotFound(payeeId.Value);

        var expectation = BillExpectation.Register(
            tenantId,
            payeeId,
            account,
            LabelFor(payee.LegalName, account),
            Recurrence.Monthly,
            bill.DueDate?.Day ?? FALLBACK_DUE_DAY,
            ObservedLeadDays(bill),
            alertLeadDays: null,
            anchorDueDate: bill.DueDate,
            item.SourceId,
            now);

        await expectations.AddAsync(expectation, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new(request.BillId, OUTCOME_CREATED, expectation.Id.Value);
    }

    private static string LabelFor(string payeeName, string account)
    {
        var label = $"{payeeName} — conta {account}";
        return label.Length > BillExpectation.LABEL_MAX_LENGTH ? label[..BillExpectation.LABEL_MAX_LENGTH] : label;
    }

    /// <summary>Quantos dias antes do vencimento esta conta chegou — o prazo que a expectativa aprende.</summary>
    private static int ObservedLeadDays(Bill bill)
        => bill.DueDate is { } due
            ? Math.Max(0, due.DayNumber - DateOnly.FromDateTime(bill.Origin.ReceivedAt).DayNumber)
            : 0;
}
