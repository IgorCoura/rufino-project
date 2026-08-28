namespace BillPayment.Application.CaptureItems.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Registra que uma tentativa de processar o artefato estourou, e decide se vale insistir.
/// </summary>
/// <param name="Permanent">
/// Se a falha é uma recusa determinística do domínio. Quem classifica é o worker, olhando o
/// tipo da exceção — não o handler, que já receberia o veredito pronto.
/// </param>
/// <remarks>
/// <para>
/// <strong>Roda num escopo NOVO, depois de a transação do processamento ter sido descartada.</strong>
/// É a única forma correta: quando o processamento estoura, o agregado em memória está no meio de
/// uma mutação — no caso medido em 2026-08-26 o item já tinha passado por <c>MarkParsed</c> antes
/// de a criação do boleto recusar — e o <c>DbContext</c> carrega alterações que não podem ser
/// gravadas. Recarregar do banco descarta esse estado sujo e parte do que de fato está persistido.
/// </para>
/// <para>
/// <strong>É <c>IMultiAggregateCommand</c></strong> porque, ao desistir, escreve também o
/// desfecho no livro-caixa da captura — e os dois têm de contar a mesma história. Um item em
/// <c>Failed</c> cujo registro ainda diga <c>Pending</c> é exatamente o sintoma que este trabalho
/// veio corrigir: o e-mail preso em "ainda não processado" para sempre.
/// </para>
/// </remarks>
public sealed record RecordCaptureItemFailureCommand(
    Guid TenantId,
    Guid CaptureItemId,
    string Error,
    bool Permanent) : ITenantScopedCommand, IRequest<RecordCaptureItemFailureResponse>, IMultiAggregateCommand;

/// <param name="GaveUp"><c>true</c> quando o item foi para <c>Failed</c> e saiu da fila.</param>
public sealed record RecordCaptureItemFailureResponse(Guid Id, bool GaveUp, int Attempts);

public sealed class RecordCaptureItemFailureCommandHandler(
    ICaptureItemRepository items,
    ICapturedMessageRepository capturedMessages,
    IOptions<CaptureRetryOptions> options,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<RecordCaptureItemFailureCommandHandler> logger)
    : IRequestHandler<RecordCaptureItemFailureCommand, RecordCaptureItemFailureResponse>
{
    private readonly CaptureRetryOptions _options = options.Value;

    public async Task<RecordCaptureItemFailureResponse> Handle(
        RecordCaptureItemFailureCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var itemId = CaptureItemId.From(request.CaptureItemId);

        var item = await items.GetAsync(tenantId, itemId, cancellationToken);

        // Item que sumiu entre a reivindicação e a falha não é erro: o desfecho `Drop` apaga o
        // item, e uma purga ou uma recaptura concorrente também. Lançar `NotFound` aqui trocaria
        // uma falha já tratada por outra, e o worker registraria a segunda no lugar da primeira.
        if (item is null)
            return new RecordCaptureItemFailureResponse(request.CaptureItemId, GaveUp: false, Attempts: 0);

        var now = clock.GetUtcNow().UtcDateTime;

        var gaveUp = item.RecordProcessingFailure(
            request.Error,
            request.Permanent,
            _options.MaxAttempts,
            _options.RetryBaseDelay,
            now);

        // O livro-caixa só é escrito quando o item desiste. Carimbar o desfecho a cada tentativa
        // faria a tela oscilar entre "pendente" e "falhou" enquanto o sistema ainda está tentando.
        if (gaveUp)
            await RecordLedgerOutcomeAsync(item, tenantId, now, cancellationToken);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        if (gaveUp)
        {
            logger.LogError(
                "Capture item {ItemId} gave up after {Attempts} attempts ({Reason}).",
                item.Id.Value,
                item.ProcessingAttempts,
                item.Reason);
        }

        return new RecordCaptureItemFailureResponse(item.Id.Value, gaveUp, item.ProcessingAttempts);
    }

    /// <summary>Faz a falha aparecer na tela de e-mails capturados, em vez de "ainda não processado".</summary>
    private async Task RecordLedgerOutcomeAsync(
        CaptureItem item,
        TenantId tenantId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var message = await capturedMessages.FindByExternalMessageIdAsync(
            tenantId, item.SourceId, item.ExternalMessageId, cancellationToken);

        // Ausência do registro não derruba nada: item ingerido antes de o livro-caixa existir
        // continua sendo processado, só não aparece na tela.
        message?.RecordOutcome(
            item.ArtifactKey,
            ArtifactOutcome.ProcessingFailed,
            item.Reason,
            item.Id,
            item.BillId,
            occurredAt);
    }
}
