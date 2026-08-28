namespace BillPayment.Domain.Notifications;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Para quem o tenant quer que os avisos vão.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque a porta de aviso recebe o tenant, não um endereço.</strong>
/// <c>INotificationSender.SendAsync</c> sempre recebeu <c>TenantId</c>, e o BC não tinha nenhum
/// dado de contato — nem cadastro de usuário, nem cliente do TenantManagement. O único e-mail
/// existente era o da <c>CaptureSource</c>, que é a caixa <em>de captura</em> e não a caixa de
/// uma pessoa. Sem este agregado, um adapter de e-mail não teria para quem enviar, e é por isso
/// que a lacuna do canal nunca foi só "falta configurar SMTP".
/// </para>
/// <para>
/// <strong>É cadastro local, não consulta ao TenantManagement.</strong> Duas razões: não pôr
/// chamada de rede no caminho do aviso — cujo modo de falha é o silêncio —, e porque quem recebe
/// alerta de conta a pagar é o financeiro, que não é necessariamente quem administra o tenant.
/// </para>
/// <para>
/// <strong>Um por tenant</strong>, garantido por índice único em <c>tenant_id</c>, no mesmo molde
/// do <c>PayerProfile</c>.
/// </para>
/// </remarks>
public sealed class TenantNotificationSettings : AggregateRoot<TenantNotificationSettingsId>
{
    public const int RECIPIENT_MAX_LENGTH = 320;

    /// <summary>
    /// Teto de destinatários. Aviso não é lista de distribuição: cada endereço custa um envio por
    /// nível de escalonamento de cada ciclo, e a rede de segurança perde utilidade quando vira
    /// ruído para muita gente ao mesmo tempo.
    /// </summary>
    public const int MAX_RECIPIENTS = 10;

    private readonly List<string> _recipients = [];

    public TenantId TenantId { get; private set; }

    /// <summary>
    /// Se o canal externo está ligado para este tenant.
    /// </summary>
    /// <remarks>
    /// <strong>Desligar não apaga o alerta</strong> — ele continua registrado no agregado da
    /// expectativa e visível em <c>GET /expectations/pending</c>. O que se desliga aqui é o
    /// envio, nunca o registro; inverter isso faria um canal indisponível apagar a memória de que
    /// o alerta existiu.
    /// </remarks>
    public bool IsEnabled { get; private set; }

    public IReadOnlyCollection<string> Recipients => _recipients.AsReadOnly();

    private TenantNotificationSettings() { }

    private TenantNotificationSettings(TenantNotificationSettingsId id) : base(id) { }

    public static TenantNotificationSettings Create(TenantId tenantId, DateTime occurredAt)
    {
        var settings = new TenantNotificationSettings(TenantNotificationSettingsId.New())
        {
            TenantId = tenantId,

            // Nasce desligada: um canal que começa ligado sem ninguém ter escolhido destinatário
            // mandaria aviso para lugar nenhum e registraria falha de envio a cada ciclo.
            IsEnabled = false,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        return settings;
    }

    /// <summary>
    /// Substitui a lista inteira de destinatários e diz se o canal fica ligado.
    /// </summary>
    /// <remarks>
    /// <strong>Substitui em vez de acrescentar</strong> porque é assim que a tela funciona — quem
    /// edita vê a lista completa e a devolve inteira. Um par adicionar/remover exigiria que o
    /// cliente reconciliasse a diferença, e a reconciliação errada tira do ar o único canal.
    /// </remarks>
    public void Configure(IEnumerable<string> recipients, bool isEnabled, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(recipients);

        var normalized = new List<string>();

        foreach (var recipient in recipients)
        {
            var address = EmailSyntax.Normalize(recipient);

            if (address.Length > RECIPIENT_MAX_LENGTH || !EmailSyntax.IsValidAddress(address))
                throw TenantNotificationSettingsErrors.InvalidRecipient(recipient ?? string.Empty);

            // Repetido não é erro de quem digitou — é o mesmo endereço colado duas vezes. Recusar
            // custaria uma mensagem de erro por um caso que a normalização já resolve.
            if (!normalized.Contains(address, StringComparer.Ordinal))
                normalized.Add(address);
        }

        if (normalized.Count > MAX_RECIPIENTS)
            throw TenantNotificationSettingsErrors.TooManyRecipients(MAX_RECIPIENTS);

        if (isEnabled && normalized.Count == 0)
            throw TenantNotificationSettingsErrors.EnabledWithoutRecipients();

        _recipients.Clear();
        _recipients.AddRange(normalized);
        IsEnabled = isEnabled;
        UpdatedAt = occurredAt;
    }

    /// <summary>Há canal externo utilizável para este tenant.</summary>
    public bool CanDeliver => IsEnabled && _recipients.Count > 0;
}
