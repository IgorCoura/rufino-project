namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.PaymentOrders;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Confere no provedor se o webhook deste tenant existe, está habilitado, com a fila corrente e
/// apontando para o endereço certo — e conserta o que não estiver.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É a reconciliação de ASSINATURA</strong>, irmã da conciliação de ORDEM que o
/// <c>ReconcilePaymentOrderCommand</c> faz. O padrão é o mesmo que a indústria consolidou para
/// webhook: ouvir o evento para saber rápido, reler a API para saber a verdade, e varrer
/// periodicamente para descobrir o que se perdeu no meio. Faltava a terceira perna.
/// </para>
/// <para>
/// <strong>Por que ela não é opcional:</strong> o outbox tem backoff FINITO (cinco tentativas,
/// ~7,5 min). Provedor fora do ar por dez minutos no instante em que o tenant vincula a chave
/// esgotava as tentativas e o webhook nunca mais era provisionado — em silêncio, para sempre.
/// Esta varredura é o que transforma "nunca mais" em "no próximo ciclo".
/// </para>
/// <para>
/// <strong>Decide, não executa.</strong> Quem fala com o provedor para criar e consertar é o
/// <c>ProvisionPaymentWebhookCommand</c>; aqui só se escolhe entre rotacionar, curar e deixar
/// como está. Separar os dois é o que impede a gravação do cofre de existir em dois lugares.
/// </para>
/// </remarks>
public sealed record SweepPaymentWebhookCommand(Guid TenantId)
    : ITenantScopedCommand, IRequest<SweepPaymentWebhookResponse>;

public sealed record SweepPaymentWebhookResponse(string Outcome, string? ReasonCode);

public sealed class SweepPaymentWebhookCommandHandler(
    IPayerProfileRepository payerProfiles,
    IPaymentWebhookProvisioner provisioner,
    IPaymentOrderWorkQueries paymentOrders,
    IMediator mediator,
    IOptions<PaymentWebhookOptions> webhookOptions,
    IOptions<PaymentWebhookSweepOptions> sweepOptions,
    TimeProvider clock,
    ILogger<SweepPaymentWebhookCommandHandler> logger)
    : IRequestHandler<SweepPaymentWebhookCommand, SweepPaymentWebhookResponse>
{
    /// <summary>Estava entregando e continua entregando. O desfecho comum, e o mais silencioso.</summary>
    public const string OUTCOME_HEALTHY = "Healthy";

    /// <summary>Não existia webhook nenhum — criado agora, com token novo.</summary>
    public const string OUTCOME_PROVISIONED = "Provisioned";

    /// <summary>Existia no perfil mas sumiu do provedor — recriado, com token novo.</summary>
    public const string OUTCOME_RECREATED = "Recreated";

    /// <summary>Existia e estava quebrado (fila pausada, desabilitado, URL velha) — consertado sem trocar o token.</summary>
    public const string OUTCOME_HEALED = "Healed";

    /// <summary>Estava íntegro e o token venceu a validade combinada — trocado por higiene.</summary>
    public const string OUTCOME_ROTATED = "Rotated";

    /// <summary>Não deu para saber: provedor fora do ar, credencial irresolvível. Volta no próximo ciclo.</summary>
    public const string OUTCOME_UNAVAILABLE = "Unavailable";

    /// <summary>Não havia o que varrer: sem conta vinculada, sem URL pública configurada.</summary>
    public const string OUTCOME_SKIPPED = "Skipped";

    public async Task<SweepPaymentWebhookResponse> Handle(
        SweepPaymentWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        if (!profile.CanSchedulePayments)
            return new SweepPaymentWebhookResponse(OUTCOME_SKIPPED, "tenant_key_not_configured");

        var settings = webhookOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.PublicBaseUrl))
        {
            logger.LogWarning(
                "PaymentWebhook:PublicBaseUrl não configurada; a varredura não tem endereço para conferir "
                + "e a instalação inteira depende da conciliação por polling.");

            return new SweepPaymentWebhookResponse(OUTCOME_SKIPPED, "public_base_url_missing");
        }

        var expectedUrl = settings.BuildCallbackUrl(request.TenantId);

        // Perfil sem webhook é o caso do outbox esgotado — e o motivo número um desta varredura
        // existir. Não há saúde a consultar: não há o que consultar.
        if (!profile.HasWebhook)
        {
            logger.LogWarning(
                "O tenant {TenantId} tem conta vinculada e nenhum webhook provisionado; provisionando agora.",
                request.TenantId);

            return await ActuateAsync(request.TenantId, rotateToken: true, OUTCOME_PROVISIONED, cancellationToken);
        }

        var health = await provisioner.GetHealthAsync(
            profile.AsaasAccountRef, profile.AsaasWebhookId!, cancellationToken);

        if (health.IsUnavailable)
        {
            // Indisponibilidade NÃO vira conserto: recriar por não conseguir ler deixaria um
            // webhook órfão na conta do tenant a cada instabilidade do provedor.
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Não foi possível conferir a saúde do webhook do tenant {TenantId} ({Reason}); "
                    + "nova tentativa no próximo ciclo.",
                    request.TenantId, health.ReasonCode);
            }

            return new SweepPaymentWebhookResponse(OUTCOME_UNAVAILABLE, health.ReasonCode);
        }

        if (!health.Found)
        {
            // Apagado no painel, ou a conta trocou por baixo do vínculo. O token do cofre já não
            // vale para webhook nenhum: recriar é rotacionar.
            logger.LogWarning(
                "O webhook {WebhookId} do tenant {TenantId} não existe mais no provedor; recriando.",
                profile.AsaasWebhookId, request.TenantId);

            return await ActuateAsync(request.TenantId, rotateToken: true, OUTCOME_RECREATED, cancellationToken);
        }

        if (!health.IsDelivering(expectedUrl))
        {
            // Fila pausada, desabilitado, ou apontando para um domínio antigo. O mesmo PUT
            // conserta os três, e o token continua valendo — curar não é rotacionar.
            logger.LogWarning(
                "O webhook do tenant {TenantId} não está entregando (habilitado={Enabled}, "
                + "fila interrompida={Interrupted}, urlCorreta={UrlMatches}); consertando.",
                request.TenantId,
                health.Enabled,
                health.Interrupted,
                string.Equals(health.Url, expectedUrl, StringComparison.OrdinalIgnoreCase));

            return await ActuateAsync(request.TenantId, rotateToken: false, OUTCOME_HEALED, cancellationToken);
        }

        var options = sweepOptions.Value;

        // Penalidade crescendo é a antessala da fila pausada — o provedor interrompe depois de
        // 15 falhas consecutivas. Gritar aqui é o que dá chance de consertar o endpoint ANTES
        // de os eventos começarem a expirar na fila dele.
        if (health.PenalizedRequestsCount >= options.PenalizedRequestsAlertThreshold)
        {
            logger.LogWarning(
                "O provedor já contou {Penalized} entregas falhas no webhook do tenant {TenantId}. "
                + "A fila é interrompida após falhas repetidas, e evento represado é descartado "
                + "depois de 14 dias — verifique se esta API está respondendo 200.",
                health.PenalizedRequestsCount, request.TenantId);
        }

        var now = clock.GetUtcNow().UtcDateTime;

        await WarnWhenSilentAsync(request.TenantId, profile, now, options, cancellationToken);

        if (profile.IsWebhookRotationDue(now, options.TokenRotationInterval))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "O token do webhook do tenant {TenantId} venceu a validade de higiene; rotacionando.",
                    request.TenantId);
            }

            return await ActuateAsync(request.TenantId, rotateToken: true, OUTCOME_ROTATED, cancellationToken);
        }

        return new SweepPaymentWebhookResponse(OUTCOME_HEALTHY, null);
    }

    /// <summary>
    /// Manda o atuador agir e traduz o desfecho dele para o vocabulário da varredura.
    /// </summary>
    /// <remarks>
    /// O atuador LANÇA quando o provedor está fora do ar (é o contrato dele com o outbox); aqui
    /// isso não pode subir: uma indisponibilidade transitória derrubaria o ciclo e os tenants
    /// seguintes ficariam sem varredura. Vira <c>Unavailable</c> e volta no próximo.
    /// </remarks>
    private async Task<SweepPaymentWebhookResponse> ActuateAsync(
        Guid tenantId,
        bool rotateToken,
        string successOutcome,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(
                new ProvisionPaymentWebhookCommand(tenantId, rotateToken), cancellationToken);

            return result.Outcome == ProvisionPaymentWebhookCommandHandler.OUTCOME_PROVISIONED
                ? new SweepPaymentWebhookResponse(successOutcome, null)
                : new SweepPaymentWebhookResponse(result.Outcome, result.ReasonCode);
        }
        catch (DomainException ex) when (ex.Id == PayerProfileErrors.ASAAS_PROVIDER_UNREACHABLE_ID)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    ex,
                    "O provedor está indisponível para consertar o webhook do tenant {TenantId}; "
                    + "nova tentativa no próximo ciclo.",
                    tenantId);
            }

            return new SweepPaymentWebhookResponse(OUTCOME_UNAVAILABLE, "provider_unreachable");
        }
    }

    /// <summary>
    /// Grita quando o webhook está mudo <strong>e há ordem viva esperando notícia</strong>.
    /// </summary>
    /// <remarks>
    /// A segunda metade da condição é o que separa alerta de ruído: tenant sem ordem em curso
    /// fica meses sem receber evento nenhum, e isso é o funcionamento normal. A contagem só é
    /// consultada depois de o silêncio já estar caracterizado — não se paga a query por ciclo.
    /// </remarks>
    private async Task WarnWhenSilentAsync(
        Guid tenantId,
        PayerProfile profile,
        DateTime nowUtc,
        PaymentWebhookSweepOptions options,
        CancellationToken cancellationToken)
    {
        if (!profile.IsWebhookSilentSince(nowUtc, options.SilenceTolerance))
            return;

        var awaiting = await paymentOrders.CountAwaitingProviderAsync(tenantId, cancellationToken);
        if (awaiting == 0)
            return;

        logger.LogWarning(
            "O webhook do tenant {TenantId} está íntegro no provedor mas não entrega evento desde "
            + "{LastEventAt}, com {Awaiting} ordem(ns) esperando desfecho. A conciliação segue "
            + "vigiando, com o atraso do StaleAfter dela.",
            tenantId, profile.LastWebhookEventAt, awaiting);
    }
}

/// <summary>O ritmo e as réguas da varredura de webhooks. A decisão em si mora no comando.</summary>
public sealed class PaymentWebhookSweepOptions
{
    public const string SectionName = "PaymentWebhookSweep";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// De quanto em quanto tempo a instalação inteira é reconferida.
    /// </summary>
    /// <remarks>
    /// Meia hora é uma leitura por tenant por ciclo — barato — e limita a quanto tempo um webhook
    /// pode ficar quebrado sem ninguém saber. Mais frequente que isso não compra nada: quem
    /// segura o dinheiro nesse meio-tempo é a conciliação, que roda a cada dez minutos.
    /// </remarks>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Quantos tenants por ciclo. Zero desliga na prática.</summary>
    public int BatchSize { get; set; } = 200;

    /// <summary>
    /// De quanto em quanto tempo o token é trocado por higiene.
    /// </summary>
    /// <remarks>
    /// <strong>Trimestral, não diário.</strong> O token do provedor não expira sozinho — ao
    /// contrário do bearer de service account que o PeopleManagement embute no webhook da
    /// ZapSign, e que é a razão de LÁ a rotação ser diária. Aqui cada rotação é uma janela de
    /// risco (o provedor não devolve o token em leitura nenhuma), então rotacionar mais é
    /// arriscar mais sem ganhar nada.
    /// </remarks>
    public TimeSpan TokenRotationInterval { get; set; } = TimeSpan.FromDays(90);

    /// <summary>A partir de quanto silêncio vale perguntar se o webhook morreu.</summary>
    public TimeSpan SilenceTolerance { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// A partir de quantas entregas penalizadas o alerta sobe. O provedor interrompe a fila em
    /// 15 falhas consecutivas — cinco é cedo o bastante para dar tempo de agir.
    /// </summary>
    public int PenalizedRequestsAlertThreshold { get; set; } = 5;
}
