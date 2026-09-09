namespace BillPayment.Application.PaymentOrders.Commands;

using System.Security.Cryptography;
using System.Text;
using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Um evento de webhook do provedor, para um tenant.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O payload é tratado como AVISO, não como verdade.</strong> O provedor não assina o
/// corpo — oferece apenas um token no header, sem HMAC —, então um corpo não prova nada além de
/// que alguém conhece o token. O handler usa o payload só para <em>descobrir de qual ordem se
/// trata</em> e em seguida <strong>relê a ordem no provedor</strong> com a chave do tenant,
/// aplicando o que a leitura afirmar. É o que transforma um corpo forjado, no pior caso, numa
/// chamada de API desperdiçada.
/// </para>
/// <para>
/// <strong>Duas chaves de resolução, porque o provedor emite duas famílias.</strong> No
/// pague-contas o objeto <c>bill</c> traz a <c>externalReference</c> que gravamos (o id da
/// ordem). Na saída de Pix não existe evento de transação — <strong>medido em sandbox em
/// 2026-09-08</strong>: os únicos eventos são <c>TRANSFER_*</c>, cujo objeto é o <c>transfer</c>
/// espelho, com id próprio e <c>externalReference</c> sempre nulo. Daí a busca por
/// <c>ProviderTransferId</c>.
/// </para>
/// </remarks>
public sealed record ProcessAsaasWebhookCommand(
    Guid TenantId,
    string PresentedToken,
    string EventId,
    string EventName,
    string? ExternalReference,
    string? ProviderObjectId,
    string? PayloadStatus) : IRequest<ProcessAsaasWebhookResponse>, ISensitiveCommand;

public sealed record ProcessAsaasWebhookResponse(string Outcome);

public sealed class ProcessAsaasWebhookCommandHandler(
    IPaymentOrderRepository orders,
    IPayerProfileRepository payerProfiles,
    IPaymentWebhookLedger ledger,
    ISecretVault vault,
    IBillPaymentGateway billGateway,
    IPixPaymentGateway pixGateway,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<ProcessAsaasWebhookCommandHandler> logger)
    : IRequestHandler<ProcessAsaasWebhookCommand, ProcessAsaasWebhookResponse>
{
    public const string OUTCOME_UNAUTHORIZED = "Unauthorized";

    /// <summary>
    /// Chegou evento para um tenant que não tem webhook nosso — tipicamente porque a conta dele
    /// acabou de ser desvinculada e o provedor ainda tem entregas na fila.
    /// </summary>
    /// <remarks>
    /// <strong>É 200, e a diferença para o 401 é de vida ou morte da fila.</strong> O provedor
    /// só considera sucesso o HTTP 200 e interrompe a fila SEQUENCIAL da conta depois de 15
    /// falhas consecutivas — evento represado ali é descartado em 14 dias. Devolver 401 para
    /// "não tenho webhook" faria um desvínculo pausar a conta do cliente em quinze entregas, e
    /// nada disso é forjadura: é o provedor esvaziando a fila dele.
    /// </remarks>
    public const string OUTCOME_NOT_CONFIGURED = "NotConfigured";

    /// <summary>
    /// O provedor afirmou um retrato que não fecha (hoje: pago sem data de pagamento).
    /// </summary>
    /// <remarks>
    /// <strong>É 200</strong>, e a marca do ledger persiste no mesmo save — devolver não-2xx
    /// faria o provedor reentregar o mesmo evento para sempre e represar a fila da conta.
    /// </remarks>
    public const string OUTCOME_INCOHERENT = "Incoherent";

    private const string OUTCOME_APPLIED = "Applied";
    private const string OUTCOME_IGNORED = "Ignored";
    private const string OUTCOME_DUPLICATE = "Duplicate";
    private const string OUTCOME_UNKNOWN = "Unknown";
    private const string OUTCOME_UNVERIFIABLE = "Unverifiable";

    public async Task<ProcessAsaasWebhookResponse> Handle(
        ProcessAsaasWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);

        // Sem webhook cadastrado não há segredo com que conferir — e não conferir significa não
        // aplicar nada. Mas o desfecho é 200: ver OUTCOME_NOT_CONFIGURED.
        if (profile is null || !profile.HasWebhook)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Webhook {EventName} recebido para um tenant sem webhook configurado; absorvido sem efeito.",
                    request.EventName);
            }

            return new ProcessAsaasWebhookResponse(OUTCOME_NOT_CONFIGURED);
        }

        // O 401 sobrou para o que ele sempre quis dizer: alguém apresentou o token ERRADO de um
        // webhook que existe. Represar a fila de quem tenta forjar é o comportamento certo.
        if (!await IsTokenValidAsync(profile, request.PresentedToken, cancellationToken))
            return new ProcessAsaasWebhookResponse(OUTCOME_UNAUTHORIZED);

        // A marca é POR TENANT desde 2026-09-08: com uma conta de provedor por tenant, dois ids
        // de evento iguais vindos de contas diferentes descartariam um evento em silêncio.
        var ledgerKey = $"{request.TenantId:N}:{request.EventId}";
        if (await ledger.ExistsAsync(ledgerKey, cancellationToken))
            return new ProcessAsaasWebhookResponse(OUTCOME_DUPLICATE);

        var nowUtc = clock.GetUtcNow();
        var now = nowUtc.UtcDateTime;

        await ledger.RecordAsync(ledgerKey, now, cancellationToken);

        // Sinal de vida da conta: é isto que permite distinguir "nada aconteceu" de "o webhook
        // desta conta morreu" sem esperar um boleto não andar.
        profile.RecordWebhookActivity(now);

        var order = await ResolveOrderAsync(request, cancellationToken);
        if (order is null)
        {
            // A marca persiste mesmo assim: o mesmo evento reentregue amanhã continua não sendo
            // nosso. Falhar faria o provedor reentregar para sempre e represar a fila da conta.
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Webhook {EventName} sem ordem correspondente; ignorado.", request.EventName);

            return new ProcessAsaasWebhookResponse(OUTCOME_UNKNOWN);
        }

        // A RELEITURA. Nada do corpo do evento decide estado: o provedor é consultado com a chave
        // do tenant e é a resposta dele que vale.
        var providerOrderId = order.ProviderOrderId;
        if (providerOrderId is null)
        {
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return new ProcessAsaasWebhookResponse(OUTCOME_IGNORED);
        }

        var fetch = await order.GetFromProviderAsync(
            billGateway, pixGateway, profile.AsaasAccountRef, providerOrderId, cancellationToken);

        if (fetch.IsUnavailable)
        {
            // Não dá para confirmar agora. A marca persiste (o evento foi recebido) e a
            // conciliação continua vigiando a ordem — é exatamente para isto que ela existe.
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogWarning(
                "Webhook {EventName} recebido, mas a releitura da ordem {PaymentOrderId} falhou ({Reason}). "
                + "A conciliação segue vigiando.",
                request.EventName, order.Id.Value, fetch.ReasonCode);

            return new ProcessAsaasWebhookResponse(OUTCOME_UNVERIFIABLE);
        }

        if (!fetch.IsFound)
        {
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return new ProcessAsaasWebhookResponse(OUTCOME_IGNORED);
        }

        var snapshot = fetch.Snapshot!;

        order.RecordProviderDiagnostics(snapshot.RawStatus, snapshot.Authorized, snapshot.TransferId, now);

        bool applied;
        try
        {
            applied = order.ApplyProviderStatus(
                snapshot.Status, snapshot.PaidAt, snapshot.Fee, snapshot.FailReasons, nowUtc, now);
        }
        catch (DomainException ex) when (ex.Id == PaymentOrderErrors.INCOHERENT_PROVIDER_PAYLOAD_ID)
        {
            // 200, e a marca do ledger PERSISTE. Deixar o PMO03 subir daqui vira não-2xx, e o
            // provedor interrompe a fila SEQUENCIAL da conta inteira depois de 15 falhas
            // consecutivas — descartando o represado em 14 dias. Um retrato incoerente não vale
            // o preço de calar a conta de um cliente; a conciliação continua vigiando a ordem.
            logger.LogError(
                ex,
                "Webhook {EventName}: o provedor descreve a ordem {PaymentOrderId} como {RawStatus}, "
                + "mas o retrato é incoerente e nada foi aplicado.",
                request.EventName, order.Id.Value, snapshot.RawStatus);

            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return new ProcessAsaasWebhookResponse(OUTCOME_INCOHERENT);
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        // Um "pago" chegando numa ordem que aqui já morreu é o pior descompasso possível —
        // dinheiro saiu no provedor com o espelho dizendo Cancelled/Failed. Nunca em silêncio.
        if (!applied && snapshot.Status == PaymentOrderStatus.Paid && order.Status.IsTerminal)
        {
            logger.LogWarning(
                "Webhook {EventName} diz PAGO, mas a ordem {PaymentOrderId} está terminal em {Status}. "
                + "Verifique o provedor: pode haver pagamento vivo para uma ordem encerrada.",
                request.EventName, order.Id.Value, order.Status.Name);
        }

        return new ProcessAsaasWebhookResponse(applied ? OUTCOME_APPLIED : OUTCOME_IGNORED);
    }

    /// <summary>
    /// Compara o token apresentado com o do tenant, em tempo constante.
    /// </summary>
    /// <remarks>
    /// Sem HMAC do provedor, este token é a única prova de origem que existe — e comparação
    /// ingênua vazaria o segredo pelo tempo de resposta, byte a byte. O valor NUNCA é logado.
    /// </remarks>
    private async Task<bool> IsTokenValidAsync(
        PayerProfile profile,
        string presented,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(presented) || profile.AsaasWebhookRef is null)
            return false;

        string expected;
        try
        {
            expected = await vault.ResolveAsync(profile.AsaasWebhookRef, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Cofre indisponível NEGA. É o lado seguro: aceitar por não conseguir conferir
            // transformaria uma falha de infraestrutura em porta aberta.
            logger.LogError(ex, "Não foi possível resolver o token de webhook do tenant no cofre.");
            return false;
        }

        var left = Encoding.UTF8.GetBytes(presented);
        var right = Encoding.UTF8.GetBytes(expected);

        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    /// <summary>
    /// Descobre de qual ordem o evento fala — a única coisa que o payload não-assinado decide.
    /// </summary>
    private async Task<PaymentOrder?> ResolveOrderAsync(
        ProcessAsaasWebhookCommand request,
        CancellationToken cancellationToken)
    {
        // Pague-contas: a externalReference É o id da ordem, e o provedor a preserva.
        if (!string.IsNullOrWhiteSpace(request.ExternalReference))
        {
            var byReference = await orders.GetByExternalReferenceAsync(
                request.ExternalReference, cancellationToken);

            if (byReference is not null)
                return byReference;
        }

        // Pix: o evento é TRANSFER_*, o objeto é o transfer espelho, e o id dele é o que
        // gravamos em ProviderTransferId na submissão.
        if (!string.IsNullOrWhiteSpace(request.ProviderObjectId))
        {
            return await orders.GetByProviderTransferIdAsync(request.ProviderObjectId, cancellationToken)
                ?? await orders.GetByExternalReferenceAsync(request.ProviderObjectId, cancellationToken);
        }

        return null;
    }
}
