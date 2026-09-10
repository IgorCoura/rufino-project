namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Payees;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using Microsoft.Extensions.Logging;

/// <summary>
/// Consulta o documento nas fontes oficiais, apura as catorze verificações e deixa o boleto
/// aguardando aprovação, com o risco classificado.
/// </summary>
/// <remarks>
/// Disparado pelo outbox a partir de <c>BillCapturedDomainEvent</c>, e também pela revalidação
/// manual — o mesmo comando serve aos dois, porque revalidar é literalmente rodar de novo.
/// </remarks>
public sealed record ValidateBillCommand(Guid TenantId, Guid BillId) : ITenantScopedCommand, IRequest<ValidateBillResponse>;

/// <param name="Risk">
/// Como o boleto ficou classificado. Entrou em 2026-09-08 (ADR-020) porque as duas contagens
/// deixaram de descrever o desfecho: com falha advisory, aviso e inconclusivo valendo Perigo,
/// "zero bloqueios" passou a conviver com <c>Danger</c>.
/// </param>
/// <param name="LookupResolved">
/// Algum trilho teve resposta da consulta oficial nesta rodada. É o que a varredura de
/// revalidação lê para saber se o boleto saiu da fila ou se o provedor continua fora — sem ele,
/// o worker teria de deduzir isso do nível de risco, que confunde "não consegui verificar" com
/// "verifiquei e é ruim".
/// </param>
public sealed record ValidateBillResponse(
    Guid Id, string Status, string Risk, int BlockingFailures, int AttentionItems, bool LookupResolved);

/// <summary>
/// O handler faz <strong>orquestração</strong> e nada mais: consulta as portas, carrega os
/// cadastros, entrega tudo ao Domain Service e passa o resultado ao agregado.
/// </summary>
/// <remarks>
/// <para>
/// Quem decide o status é <c>Bill.RecordChecks</c>; quem apura é <c>BillValidationService</c>.
/// O handler não tem um <c>if</c> sobre estado de domínio, não compõe Value Object e não lê
/// <c>bill.Checks</c> para montar a resposta — o método rico devolve o resumo.
/// </para>
/// <para>
/// <strong>A data e a hora entram por aqui</strong>, uma vez, e viajam no contexto. O Domain
/// Service não lê relógio: é o que torna a apuração inteira reprodutível em teste.
/// </para>
/// </remarks>
public sealed class ValidateBillCommandHandler(
    IBillRepository bills,
    IPayeeRepository payees,
    IBillExpectationRepository expectations,
    ITrustedOriginRepository origins,
    IPayerProfileRepository payerProfiles,
    IBillLookupService billLookup,
    IPixLookupService pixLookup,
    IBankDirectory bankDirectory,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ValidateBillCommand, ValidateBillResponse>
{
    public async Task<ValidateBillResponse> Handle(ValidateBillCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var billId = BillId.From(request.BillId);

        var bill = await bills.GetAsync(tenantId, billId, cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        var now = clock.GetUtcNow();

        // O perfil sobe ANTES da consulta: é dele que sai o ponteiro da subconta do tenant —
        // a consulta oficial é por tenant (doc 07), e sem chave própria ela degrada para
        // indisponível em vez de usar credencial de outra conta.
        var payerProfile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);

        var (bankSlipResult, pixResult) = await ConsultAsync(
            bill, payerProfile?.AsaasAccountRef, now, cancellationToken);
        bill.AttachLookups(bankSlipResult, pixResult, now.UtcDateTime);

        var tenantPayees = await payees.ListByTenantAsync(tenantId, cancellationToken);
        var resolution = PayeeResolutionService.Resolve(bill.Beneficiary, tenantPayees);
        bill.ResolvePayee(resolution.Payee?.Id, now.UtcDateTime);

        var probe = await ProbeDuplicateAsync(bill, tenantId, cancellationToken);

        var context = new BillValidationContext
        {
            Bill = bill,
            BankSlipLookup = bankSlipResult,
            PixLookup = pixResult,
            PayeeResolution = resolution,
            Origin = await ResolveOriginAsync(bill, tenantId, cancellationToken),
            PayerProfile = payerProfile,
            BankDirectory = bankDirectory,
            Expectations = await ListExpectationsAsync(bill, tenantId, cancellationToken),
            Duplicate = DuplicateFinding.From(probe),
            DuplicateOf = probe.OriginalBillId,
            Today = DateOnly.FromDateTime(now.UtcDateTime),
        };

        var outcome = bill.RecordChecks(BillValidationService.Evaluate(context), now.UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ValidateBillResponse(
            bill.Id.Value, outcome.Status.Name, outcome.Risk.Name,
            outcome.BlockingFailures, outcome.AttentionItems,
            LookupResolved: bankSlipResult?.IsResolved == true || pixResult?.IsResolved == true);
    }

    /// <summary>
    /// Consulta cada trilho presente no documento. Os dois quando há os dois — é o que permite
    /// comparar as duas histórias, que é a defesa contra QR colado sobre boleto verdadeiro.
    /// </summary>
    private async Task<(BillLookupResult? BankSlip, PixLookupResult? Pix)> ConsultAsync(
        Bill bill,
        CredentialRef? credential,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        BillLookupResult? bankSlip = null;
        PixLookupResult? pix = null;

        foreach (var instrument in bill.Instruments)
        {
            if (instrument.Kind == PaymentInstrumentKind.Barcode)
            {
                bankSlip = await billLookup.SimulateAsync(
                    credential, instrument.DigitableLine, cancellationToken);
                continue;
            }

            // A data prevista é hoje: nesta fase não há agendamento ainda, e informar a data
            // de hoje é o que faz a instituição devolver o valor que seria debitado agora.
            pix = await pixLookup.DecodeAsync(
                credential,
                instrument.PixPayload,
                DateOnly.FromDateTime(now.UtcDateTime),
                cancellationToken);
        }

        return (bankSlip, pix);
    }

    /// <summary>
    /// As contas esperadas do beneficiário que a resolução acabou de escolher.
    /// </summary>
    /// <remarks>
    /// Depois de <c>ResolvePayee</c>, de propósito: sem beneficiário não há contra o quê
    /// perguntar, e a verificação 14 já sabe dizer isso sozinha. Quem casa é o Domain Service.
    /// </remarks>
    private async Task<IReadOnlyCollection<BillExpectation>> ListExpectationsAsync(
        Bill bill, TenantId tenantId, CancellationToken cancellationToken)
        => bill.PayeeId is { } payeeId
            ? await expectations.ListByPayeeAsync(tenantId, payeeId, cancellationToken)
            : [];

    private Task<TrustedOrigin?> ResolveOriginAsync(Bill bill, TenantId tenantId, CancellationToken cancellationToken)
        => string.IsNullOrWhiteSpace(bill.Origin.SenderAddress)
            ? Task.FromResult<TrustedOrigin?>(null)
            : origins.ResolveBySenderAsync(tenantId, bill.Origin.SenderAddress, cancellationToken);

    // Sem chave de dedup não há o que perguntar ao banco. Não é decisão de domínio: quem lê
    // "sem chave" como verificação inconclusiva é o BillValidationService, que olha o
    // Bill.DedupKey por conta própria.
    private Task<DuplicateProbe> ProbeDuplicateAsync(Bill bill, TenantId tenantId, CancellationToken cancellationToken)
        => bill.DedupKey is null
            ? Task.FromResult(DuplicateProbe.NotFound())
            : bills.ProbeActiveDuplicateAsync(bill.DedupKey, tenantId, bill.Id, cancellationToken);
}

public sealed class ValidateBillIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ValidateBillIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ValidateBillCommand, ValidateBillResponse>(mediator, requestManager, logger)
{
    protected override ValidateBillResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, string.Empty, 0, 0, LookupResolved: false);
}
