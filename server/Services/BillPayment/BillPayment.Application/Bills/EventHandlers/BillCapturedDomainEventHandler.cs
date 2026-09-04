namespace BillPayment.Application.Bills.EventHandlers;

using BillPayment.Application.Bills.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using Microsoft.Extensions.Logging;

/// <summary>
/// Documento capturado dispara a verificação.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Vive na Application, não na Infra</strong>, apesar de o dispatcher do outbox ser da
/// Infra. O motivo é a regra de dependência: <c>Infra → Application</c> não existe (seria
/// ciclo), e este handler precisa do mediator. Como <c>IDomainEventHandler&lt;T&gt;</c> é porta
/// do Domain e o dispatcher resolve pelo contêiner, a implementação pode morar em qualquer
/// camada que o DI enxergue.
/// </para>
/// <para>
/// <strong>O outbox entrega ao-menos-uma-vez</strong>, então este handler precisa ser
/// idempotente. Ele é por construção: revalidar é a operação suportada, e
/// <c>Bill.RecordChecks</c> substitui o conjunto inteiro em vez de acumular.
/// </para>
/// <para>
/// Erro na consulta ao provedor <strong>não</strong> vira exceção aqui: o adapter devolve
/// <c>Unavailable</c>, o check reprova com motivo, e o boleto fica visível em <c>Rejected</c>
/// com botão de revalidar. Deixar a exceção subir faria a mensagem voltar para a fila e
/// consumir tentativas do dead-letter por uma condição que é resultado de negócio.
/// </para>
/// </remarks>
public sealed class BillCapturedDomainEventHandler(
    IMediator mediator,
    ILogger<BillCapturedDomainEventHandler> logger)
    : IDomainEventHandler<BillCapturedDomainEvent>
{
    public async Task HandleAsync(BillCapturedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var response = await mediator.Send(
            new ValidateBillCommand(domainEvent.TenantId.Value, domainEvent.BillId.Value),
            cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Boleto {BillId} verificado: {Status} ({Blocking} bloqueio(s), {Attention} ponto(s) de atenção)",
                domainEvent.BillId.Value, response.Status, response.BlockingFailures, response.AttentionItems);
        }
    }
}
