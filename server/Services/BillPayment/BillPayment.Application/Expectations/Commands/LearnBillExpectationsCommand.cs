namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Payees;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Olha o histórico de um beneficiário e, se ele for regular, passa a monitorá-lo.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Roda depois de um boleto ser aprovado</strong>, que é quando o histórico ganha uma
/// ocorrência confiável — boleto capturado e não aprovado ainda pode ser recusado.
/// </para>
/// <para>
/// <strong>Recusa aprender quando o histórico mostra mais de uma conta do mesmo beneficiário</strong>,
/// e avisa em vez de adivinhar. Medido no arquivo real: quatro instalações da EDP e três do DAE.
/// Uma expectativa por beneficiário seria cumprida pela primeira conta que chegasse e esconderia
/// as outras — a falha silenciosa que este mecanismo existe para impedir.
/// </para>
/// </remarks>
public sealed record LearnBillExpectationsCommand(Guid TenantId, Guid PayeeId)
    : IRequest<LearnBillExpectationsResponse>;

/// <param name="Outcome">
/// <c>Learned</c>, <c>AlreadyExists</c> ou o motivo da recusa — <c>TooFewOccurrences</c>,
/// <c>Irregular</c>, <c>MultipleAccounts</c>.
/// </param>
public sealed record LearnBillExpectationsResponse(Guid? ExpectationId, string Outcome);

public sealed class LearnBillExpectationsCommandHandler(
    IBillRepository bills,
    IPayeeRepository payees,
    IBillExpectationRepository expectations,
    INotificationSender notifications,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<LearnBillExpectationsCommandHandler> logger)
    : IRequestHandler<LearnBillExpectationsCommand, LearnBillExpectationsResponse>
{
    /// <summary>Quantos boletos do beneficiário alimentam a dedução. Cobre dois anos de conta mensal.</summary>
    private const int HISTORY_LIMIT = 24;

    private const string OUTCOME_LEARNED = "Learned";
    private const string OUTCOME_ALREADY_EXISTS = "AlreadyExists";

    public async Task<LearnBillExpectationsResponse> Handle(
        LearnBillExpectationsCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var payeeId = PayeeId.From(request.PayeeId);

        // Já monitorado sem referência de conta: não há o que aprender de novo.
        if (await expectations.ExistsAsync(tenantId, payeeId, string.Empty, cancellationToken))
            return new LearnBillExpectationsResponse(null, OUTCOME_ALREADY_EXISTS);

        var payee = await payees.GetAsync(tenantId, payeeId, cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        var history = await bills.ListByPayeeAsync(tenantId, payeeId, HISTORY_LIMIT, cancellationToken);
        var occurrences = ToOccurrences(history);

        var proposal = ExpectationLearningService.Propose(payeeId, occurrences);

        if (!proposal.IsProposal)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Histórico de beneficiário não virou expectativa: {Refusal} sobre {Count} ocorrências.",
                    proposal.Refusal!.Name,
                    proposal.ObservationCount);
            }

            // A recusa por múltiplas contas é acionável, e as outras duas não são: dizer ao
            // usuário que existem N contas do mesmo beneficiário é o que o leva a cadastrar cada
            // uma com sua referência. "Poucas ocorrências" resolve-se sozinho com o tempo.
            if (proposal.Refusal == LearningRefusal.MultipleAccounts)
                await NotifyMultipleAccountsAsync(tenantId, payee.LegalName, cancellationToken);

            return new LearnBillExpectationsResponse(null, proposal.Refusal!.Name);
        }

        var expectation = BillExpectation.Learn(
            tenantId,
            payeeId,
            payee.LegalName,
            proposal.Recurrence!,
            proposal.ExpectedDueDay,
            proposal.ObservedLeadDays,
            proposal.ObservationCount,
            hintSourceId: null,
            clock.GetUtcNow().UtcDateTime);

        await expectations.AddAsync(expectation, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new LearnBillExpectationsResponse(expectation.Id.Value, OUTCOME_LEARNED);
    }

    private Task NotifyMultipleAccountsAsync(
        TenantId tenantId, string payeeName, CancellationToken cancellationToken)
        => notifications.SendAsync(
            tenantId,
            NotificationKind.ExpectationLearned,
            new NotificationPayload(
                $"Mais de uma conta de {payeeName}",
                "O histórico mostra mais de uma conta deste beneficiário e não dá para saber qual é qual. "
                + "Cadastre cada uma com a sua referência (instalação, matrícula, conta contrato) "
                + "para que eu avise separadamente quando alguma não chegar.",
                "/expectations"),
            cancellationToken);

    /// <summary>
    /// Reduz o histórico ao que o aprendizado precisa: quando chegou e quando vencia.
    /// </summary>
    /// <remarks>
    /// Boleto sem vencimento legível fica de fora — arrecadação nem sempre traz a data no código
    /// de barras, e uma ocorrência sem data não descreve cadência nenhuma.
    /// </remarks>
    private static List<BillOccurrence> ToOccurrences(IReadOnlyCollection<Bill> history)
    {
        var occurrences = new List<BillOccurrence>();

        foreach (var bill in history)
        {
            var due = bill.Instruments
                .Where(i => i.Kind == PaymentInstrumentKind.Barcode)
                .Select(i => i.DigitableLine.DueDate)
                .FirstOrDefault(d => d is not null);

            if (due is not null)
            {
                occurrences.Add(new BillOccurrence(
                    DateOnly.FromDateTime(bill.Origin.ReceivedAt), DateOnly.FromDateTime(due.Value)));
            }
        }

        return occurrences;
    }
}

public sealed class LearnBillExpectationsIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<LearnBillExpectationsIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<LearnBillExpectationsCommand, LearnBillExpectationsResponse>(
        mediator, requestManager, logger)
{
    protected override LearnBillExpectationsResponse CreateResultForDuplicateRequest()
        => new(null, string.Empty);
}
