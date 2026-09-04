namespace BillPayment.Application.Retention.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Retention;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Apaga os registros de e-mail vencidos pela janela de retenção de um tenant.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nunca alcança registro que produziu boleto</strong>, esteja a janela em 7 ou em 180
/// dias: o registro de um e-mail que virou pagamento é trilha de auditoria, e prazo de retenção
/// de histórico operacional não é motivo para apagá-la. Quem garante isso é a própria consulta
/// (<c>ListPurgeableAsync</c>), não um <c>if</c> aqui.
/// </para>
/// <para>
/// Só o registro é apagado — os <c>CaptureItem</c> e os arquivos seguem a retenção por desfecho,
/// que é outra regra. Um e-mail purgado do histórico cujo item ainda existe continua alcançável
/// pela quarentena.
/// </para>
/// </remarks>
public sealed record PurgeCapturedMessagesCommand(Guid TenantId, int BatchSize)
    : ITenantScopedCommand, IRequest<PurgeCapturedMessagesResponse>;

/// <param name="Purged">Quantos registros saíram. Zero é o desfecho normal.</param>
public sealed record PurgeCapturedMessagesResponse(int Purged);

public sealed class PurgeCapturedMessagesCommandHandler(
    ICaptureRetentionPolicyRepository policies,
    ICapturedMessageRepository capturedMessages,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<PurgeCapturedMessagesCommandHandler> logger)
    : IRequestHandler<PurgeCapturedMessagesCommand, PurgeCapturedMessagesResponse>
{
    public async Task<PurgeCapturedMessagesResponse> Handle(
        PurgeCapturedMessagesCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var policy = await policies.GetAsync(tenantId, cancellationToken);

        // Política ausente ou desligada não purga nada. Desligado é o padrão, e é assim que quem
        // nunca abriu a tela não perde histórico sem ter escolhido.
        if (policy is null || !policy.IsEnabled)
            return new PurgeCapturedMessagesResponse(0);

        var cutoff = policy.CutoffAt(clock.GetUtcNow().UtcDateTime);

        var expired = await capturedMessages.ListPurgeableAsync(
            tenantId, cutoff, request.BatchSize, cancellationToken);

        if (expired.Count == 0)
            return new PurgeCapturedMessagesResponse(0);

        foreach (var message in expired)
        {
            // O corpo guardado expira junto com o registro — reter o blob de um e-mail purgado
            // seria manter exatamente o que a janela mandou apagar. Mensagem que produziu boleto
            // nunca chega aqui (ListPurgeableAsync a exclui), então o corpo dela sobrevive.
            if (message.HasStoredBody)
                await storage.RemoveAsync(message.TenantId, message.BodyStorageKey!, cancellationToken);

            capturedMessages.Remove(message);
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Purga de histórico de captura: {Count} registros anteriores a {Cutoff:yyyy-MM-dd} removidos.",
                expired.Count, cutoff);
        }

        return new PurgeCapturedMessagesResponse(expired.Count);
    }
}
