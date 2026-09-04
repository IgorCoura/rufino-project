namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using Microsoft.Extensions.Logging;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Devolve o boleto à fila de leitura por IA.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o backfill dos boletos nascidos antes de a leitura existir</strong> — e o botão de
/// reler depois de o prompt melhorar. O original vem do storage (guardrail 2 do doc 10:
/// reextração parte do documento guardado), o corpo vem do livro-caixa da captura.
/// </para>
/// <para>
/// <strong>Não chama o extrator — enfileira.</strong> Quem lê é o worker, serial e com
/// retentativa. Chamar aqui prenderia a requisição HTTP pela latência do provedor e faria uma
/// falha de rede voltar ao usuário como "este documento não tem o que ler".
/// </para>
/// </remarks>
public sealed record EnrichBillReadingCommand(Guid TenantId, Guid BillId)
    : ITenantScopedCommand, IRequest<EnrichBillReadingResponse>;

/// <param name="Enriched">Se a leitura trouxe conteúdo e foi anexada.</param>
/// <param name="Reason">Por que não enriqueceu, quando não enriqueceu — código estável.</param>
public sealed record EnrichBillReadingResponse(Guid Id, bool Enriched, string? Reason = null);

public sealed class EnrichBillReadingCommandHandler(
    IBillRepository bills,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<EnrichBillReadingCommand, EnrichBillReadingResponse>
{
    public async Task<EnrichBillReadingResponse> Handle(
        EnrichBillReadingCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var bill = await bills.GetAsync(tenantId, BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        // Reenfileira em vez de chamar o extrator aqui. Chamar na hora prendia a requisição HTTP
        // por até dois minutos e, quando o provedor falhava, devolvia "nada extraído" para um
        // documento que só não tinha sido lido — que é a confusão inteira que esta sprint desfez.
        // "Tentar de novo" passa a significar o que o nome diz: volta para a fila, com orçamento
        // novo de tentativas.
        if (!bill.QueueReading(clock.GetUtcNow().UtcDateTime))
            return new EnrichBillReadingResponse(request.BillId, false, "no_stored_document");

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new EnrichBillReadingResponse(request.BillId, true, "queued");
    }
}

public sealed class EnrichBillReadingIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<EnrichBillReadingIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<EnrichBillReadingCommand, EnrichBillReadingResponse>(mediator, requestManager, logger)
{
    protected override EnrichBillReadingResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, false, "duplicate_request");
}
