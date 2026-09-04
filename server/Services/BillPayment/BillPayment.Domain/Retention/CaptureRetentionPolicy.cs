namespace BillPayment.Domain.Retention;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Por quanto tempo o cliente guarda o histórico dos e-mails que não viraram boleto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Uma por tenant</strong>, como o <c>PayerProfile</c> — garantida por índice único em
/// <c>TenantId</c>.
/// </para>
/// <para>
/// <strong>Desligada por padrão, e isso é decisão e não omissão.</strong> Uma política que apaga
/// sozinha apagaria o histórico de quem nunca abriu a tela e nem sabe que ele existe. Ligar é
/// escolha explícita de quem opera; o preço de deixar desligado é o registro crescer.
/// </para>
/// <para>
/// Ela nunca alcança registro que produziu boleto — a regra vive no
/// <c>ICapturedMessageRepository.ListPurgeableAsync</c>, porque é sobre <em>quais</em> registros
/// existem, não sobre a política em si.
/// </para>
/// </remarks>
public sealed class CaptureRetentionPolicy : AggregateRoot<CaptureRetentionPolicyId>
{
    public TenantId TenantId { get; private set; }

    /// <summary>Se a purga roda. Falso é o estado inicial.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>A janela em vigor. Existe mesmo com a política desligada, para a tela lembrá-la.</summary>
    public RetentionWindow Window { get; private set; } = default!;

    private CaptureRetentionPolicy() { }

    private CaptureRetentionPolicy(CaptureRetentionPolicyId id) : base(id) { }

    /// <summary>A política de quem nunca configurou: desligada, com a janela padrão pré-escolhida.</summary>
    public static CaptureRetentionPolicy Default(TenantId tenantId, DateTime occurredAt)
        => new(CaptureRetentionPolicyId.New())
        {
            TenantId = tenantId,
            IsEnabled = false,
            Window = RetentionWindow.Default,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

    /// <summary>Liga ou desliga a purga e define a janela.</summary>
    /// <remarks>
    /// A janela é exigida mesmo ao desligar: o número continua na tela, e deixá-lo nulo faria a
    /// interface ter de inventar um valor para mostrar.
    /// </remarks>
    public void Configure(bool isEnabled, RetentionWindow window, DateTime occurredAt)
    {
        IsEnabled = isEnabled;
        Window = window ?? throw CaptureRetentionPolicyErrors.WindowRequired();
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// A data-limite: registro mais antigo que isto pode ser purgado.
    /// </summary>
    /// <remarks>
    /// Conta a partir de <c>ReceivedAt</c>, não da data do processamento — é a data que a pessoa
    /// vê na tela e a que ela usa para raciocinar sobre "os últimos 30 dias".
    /// </remarks>
    public DateTime CutoffAt(DateTime now) => now.AddDays(-Window.Days);

    /// <summary>Traduz o prazo em dias para a janela, recusando o que está fora da faixa.</summary>
    public static RetentionWindow WindowFromDays(int days)
        => Enumeration.GetAll<RetentionWindow>().FirstOrDefault(w => w.Days == days)
            ?? throw CaptureRetentionPolicyErrors.UnsupportedWindow(days);
}
