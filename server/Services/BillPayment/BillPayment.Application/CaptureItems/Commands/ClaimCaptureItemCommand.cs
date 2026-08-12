namespace BillPayment.Application.CaptureItems.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Uma pessoa assume que o boleto na quarentena é desta conta, e ele vira <c>Bill</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o degrau mais fraco da escada, e por isso o mais visível.</strong> A <c>Bill</c>
/// nasce com <c>RoutingConfidence.Claimed</c>, que faz o check <c>TenantRouting</c> sair
/// <c>Inconclusive</c> na tela de aprovação — aprovar um boleto reivindicado é decisão
/// consciente, nunca caminho silencioso (doc 07).
/// </para>
/// <para>
/// <strong>É <c>IMultiAggregateCommand</c> pelo mesmo motivo do processamento</strong>: o
/// <c>BillId</c> que o item guarda só existe depois de o boleto ser criado, então os dois têm de
/// nascer na mesma transação.
/// </para>
/// <para>
/// <strong>O artefato é relido, não é guardado destrinchado.</strong> O item da quarentena não
/// carrega instrumentos — carrega o documento. Reler pelo mesmo parser é o que garante que a
/// reivindicação passa pelos mesmos dígitos verificadores do caminho automático: quem reivindica
/// escolhe de <em>quem</em> é o boleto, nunca <em>o que</em> ele diz.
/// </para>
/// </remarks>
public sealed record ClaimCaptureItemCommand(Guid TenantId, Guid CaptureItemId, Guid UserId)
    : IRequest<ClaimCaptureItemResponse>, IMultiAggregateCommand;

public sealed record ClaimCaptureItemResponse(Guid Id, Guid BillId, string Status);

public sealed class ClaimCaptureItemCommandHandler(
    ICaptureItemRepository items,
    IBillRepository bills,
    IPayerProfileRepository payerProfiles,
    IBoletoDocumentParser parser,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ClaimCaptureItemCommand, ClaimCaptureItemResponse>
{
    public async Task<ClaimCaptureItemResponse> Handle(
        ClaimCaptureItemCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var item = await items.GetAsync(tenantId, CaptureItemId.From(request.CaptureItemId), cancellationToken)
            ?? throw CaptureItemErrors.NotFound(request.CaptureItemId);

        if (string.IsNullOrEmpty(item.StorageKey))
            throw CaptureItemErrors.ArtifactRequired();

        var now = clock.GetUtcNow();

        var content = await storage.RetrieveAsync(tenantId, item.StorageKey, cancellationToken);
        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);

        var extraction = await parser.ParseAsync(
            content,
            item.ContentType,
            PasswordDerivationService.Derive(profile),
            DateOnly.FromDateTime(now.UtcDateTime),
            cancellationToken);

        if (!extraction.Resolved)
            throw CaptureItemErrors.NoInstrumentToClaim(request.CaptureItemId);

        var bill = Bill.Capture(
            tenantId,
            extraction.Instruments,
            BillOrigin.Create(
                BillSourceKind.Mailbox,
                item.ReceivedAt,
                item.SourceId.Value,
                item.Sender,
                item.ExternalMessageId,
                item.ContentHash,
                item.StorageKey),
            now.UtcDateTime,
            extractedPayer: null,
            RoutingConfidence.Claimed);

        // Unicidade global — travessia autorizada do ADR-008. Quando outro tenant já reivindicou
        // o mesmo documento, o erro que sobe é o genérico BLP.BIL02: o usuário precisa saber que
        // o boleto está sob gestão, e não de quem. A corrida entre duas reivindicações simultâneas
        // é resolvida pelo índice único parcial, que devolve o mesmo erro.
        if (bill.DedupKey is not null
            && await bills.ExistsActiveByDedupKeyAsync(bill.DedupKey, cancellationToken))
        {
            throw BillErrors.AlreadyCaptured();
        }

        await bills.AddAsync(bill, cancellationToken);

        // A recusa por pagador contraditório (BLP.CPI04) e a transição inválida vivem dentro do
        // método rico — o handler não lê o status para decidir.
        item.Claim(UserId.From(request.UserId), bill.Id, now.UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ClaimCaptureItemResponse(item.Id.Value, bill.Id.Value, item.Status.Name);
    }
}

public sealed class ClaimCaptureItemIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ClaimCaptureItemIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ClaimCaptureItemCommand, ClaimCaptureItemResponse>(mediator, requestManager, logger)
{
    protected override ClaimCaptureItemResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, Guid.Empty, string.Empty);
}
