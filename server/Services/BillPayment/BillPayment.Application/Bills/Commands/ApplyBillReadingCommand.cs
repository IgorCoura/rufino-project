namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Application.Bills;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Payees;
using BillPayment.Domain.Ports;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using Microsoft.Extensions.Logging;

/// <summary>
/// A leitura por IA chegou: anexa o retrato e reprocessa <strong>somente</strong> o que depende dele.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O que depende da leitura, e portanto é refeito:</strong> o vencimento consolidado (a
/// leitura é a última reserva da precedência), o check 13 <c>DocumentConsistency</c> — que
/// <em>só existe</em> quando há retrato —, o <c>DueDateSanity</c>, e o nível de risco, que é
/// derivado dos checks.
/// </para>
/// <para>
/// <strong>O que NÃO é refeito:</strong> a consulta oficial (o retrato guardado basta;
/// reconsultar gastaria cota e ainda acionaria a regra de retrato velho), o roteamento (já
/// decidido, o item já foi promovido) e a chave de dedup (vem do instrumento, não da leitura).
/// É este recorte que faz "reprocessar só as partes necessárias" ser verdade e não figura de
/// linguagem.
/// </para>
/// <para>
/// <strong>Boleto já decidido NÃO é revalidado.</strong> <c>AcceptsValidation</c> inclui
/// <c>Approved</c>, e revalidar ali derruba a aprovação incondicionalmente — um enriquecimento
/// de fundo desfazendo em silêncio a decisão de uma pessoa é a pior troca possível. O retrato é
/// anexado assim mesmo (é metadado, e melhora o histórico) e o boleto fica marcado com
/// <c>ReadingArrivedAfterDecision</c>.
/// </para>
/// </remarks>
public sealed record ApplyBillReadingCommand(Guid TenantId, Guid BillId)
    : IRequest<ApplyBillReadingResponse>;

/// <param name="Outcome">
/// <c>Applied</c>, <c>AppliedWithoutRevalidation</c>, <c>NothingExtracted</c> ou o motivo de não
/// ter sido possível ler.
/// </param>
public sealed record ApplyBillReadingResponse(Guid BillId, bool Applied, string Outcome);

public sealed class ApplyBillReadingCommandHandler(
    IBillRepository bills,
    IPayeeRepository payees,
    IPayerProfileRepository payerProfiles,
    ITrustedOriginRepository trustedOrigins,
    IBankDirectory bankDirectory,
    IBillReadingSource readingSource,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<ApplyBillReadingCommandHandler> logger)
    : IRequestHandler<ApplyBillReadingCommand, ApplyBillReadingResponse>
{
    private const string OUTCOME_APPLIED = "Applied";
    private const string OUTCOME_WITHOUT_REVALIDATION = "AppliedWithoutRevalidation";
    private const string OUTCOME_NOTHING = "NothingExtracted";

    public async Task<ApplyBillReadingResponse> Handle(
        ApplyBillReadingCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var bill = await bills.GetAsync(tenantId, BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        var now = clock.GetUtcNow();

        var attempt = await readingSource.ReadAsync(bill, tenantId, cancellationToken);

        // Indisponibilidade sobe como exceção de domínio para o worker devolver o boleto à fila —
        // é o mesmo sinal que a faixa de captura usa, e é o que aciona a retentativa com espera
        // dobrando. Recusa e ausência NÃO sobem: repetir daria o mesmo e gastaria cota.
        if (attempt.Status.IsRetryable)
            throw BillErrors.ReadingUnavailable(attempt.ReasonCode);

        if (!attempt.HasReading)
            return await GiveAsync(bill, request.BillId, OUTCOME_NOTHING, now.UtcDateTime, cancellationToken);

        var canRevalidate = bill.AcceptsSilentRevalidation;

        bill.AttachReading(attempt.Reading!, now.UtcDateTime);

        if (!canRevalidate)
        {
            bill.MarkReadingArrivedAfterDecision(now.UtcDateTime);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Retrato anexado a um boleto já decidido; a verificação NÃO foi refeita para "
                    + "não desfazer a decisão.");
            }

            return new ApplyBillReadingResponse(request.BillId, true, OUTCOME_WITHOUT_REVALIDATION);
        }

        await RevalidateAsync(bill, tenantId, now, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ApplyBillReadingResponse(request.BillId, true, OUTCOME_APPLIED);
    }

    /// <summary>
    /// Refaz a apuração dos checks <strong>sobre os retratos oficiais já armazenados</strong>.
    /// </summary>
    /// <remarks>
    /// Reconsultar o provedor aqui seria caro e errado: a consulta já foi feita, o retrato está
    /// guardado, e o que mudou foi o documento lido — não o que o banco respondeu.
    /// </remarks>
    private async Task RevalidateAsync(
        Bill bill, TenantId tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantPayees = await payees.ListByTenantAsync(tenantId, cancellationToken);
        var resolution = PayeeResolutionService.Resolve(bill.Beneficiary, tenantPayees);

        var context = new BillValidationContext
        {
            Bill = bill,
            BankSlipLookup = StoredBankSlipLookup(bill, now),
            PixLookup = StoredPixLookup(bill, now),
            PayeeResolution = resolution,
            Origin = await ResolveOriginAsync(bill, tenantId, cancellationToken),
            PayerProfile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken),
            BankDirectory = bankDirectory,
            Today = DateOnly.FromDateTime(now.UtcDateTime),
            TimeOfDay = TimeOnly.FromDateTime(now.UtcDateTime),
        };

        bill.RecordChecks(BillValidationService.Evaluate(context), now.UtcDateTime);
    }

    /// <summary>
    /// O retrato de cobrança já guardado, reapresentado como resultado resolvido.
    /// </summary>
    /// <remarks>
    /// Sem retrato guardado a consulta é declarada <c>Unavailable</c> — e não <c>Unresolved</c>:
    /// a segunda afirmaria que o provedor não conhece o título, o que nunca foi apurado aqui.
    /// </remarks>
    private static BillLookupResult StoredBankSlipLookup(Bill bill, DateTimeOffset now)
        => bill.Lookup is { } snapshot
            ? BillLookupResult.Resolved(snapshot, now)
            : BillLookupResult.Unavailable("not_consulted_on_reading", null, now);

    private static PixLookupResult StoredPixLookup(Bill bill, DateTimeOffset now)
        => bill.PixLookup is { } snapshot
            ? PixLookupResult.Resolved(snapshot, now)
            : PixLookupResult.Unavailable("not_consulted_on_reading", null, now);

    private async Task<TrustedOrigin?> ResolveOriginAsync(
        Bill bill, TenantId tenantId, CancellationToken cancellationToken)
        => string.IsNullOrEmpty(bill.Origin.SenderAddress)
            ? null
            : await trustedOrigins.ResolveBySenderAsync(tenantId, bill.Origin.SenderAddress, cancellationToken);

    /// <summary>Nada foi extraído: o boleto sai da fila sem retrato, e isso é desfecho, não erro.</summary>
    /// <remarks>
    /// <strong>O <c>SaveEntitiesAsync</c> aqui não é zelo — é o que faz o boleto SAIR da fila.</strong>
    /// A saída da fila é uma mutação como qualquer outra (<c>ReadingState</c> vira
    /// <c>Unavailable</c>), e sem persistir ela o boleto permanece <c>Queued</c>: o aluguel vence
    /// em quinze minutos, a reivindicação o pega de novo, o extrator devolve o mesmo nada, e o
    /// ciclo recomeça — para sempre, gastando cota do provedor a cada volta. É o modo de falha
    /// oposto ao da fila parada, e igualmente silencioso.
    /// </remarks>
    private async Task<ApplyBillReadingResponse> GiveAsync(
        Bill bill,
        Guid billId,
        string outcome,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        bill.RecordReadingFailure(permanent: true, maxAttempts: 0, TimeSpan.Zero, occurredAt);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ApplyBillReadingResponse(billId, false, outcome);
    }
}

public sealed class ApplyBillReadingIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ApplyBillReadingIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ApplyBillReadingCommand, ApplyBillReadingResponse>(
        mediator, requestManager, logger)
{
    protected override ApplyBillReadingResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, false, string.Empty);
}
