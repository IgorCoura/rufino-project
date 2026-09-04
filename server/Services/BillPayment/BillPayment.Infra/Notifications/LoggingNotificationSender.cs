namespace BillPayment.Infra.Notifications;

using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Entrega o aviso no log estruturado, enquanto não há canal externo configurado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Não é substituto que falha alto, e a diferença é deliberada.</strong> O cofre e o
/// armazenamento falham quando não configurados porque guardar em lugar nenhum sem avisar é pior
/// que parar. Aviso é o contrário: derrubar a varredura porque o e-mail não está configurado
/// apagaria também o <em>registro</em> do alerta, e é o registro que sustenta o painel de
/// pendências e a regra de não repetir nível. O alerta continua existindo no agregado, visível em
/// <c>GET /expectations/pending</c>, mesmo que ninguém receba e-mail.
/// </para>
/// <para>
/// <strong>O aviso não carrega instrumento de pagamento</strong>, e é por isso que ele pode ir
/// para o log. Quem monta o texto é a Application, e o contrato da porta já diz que linha
/// digitável e BR Code não atravessam.
/// </para>
/// <para>
/// O adapter de e-mail de verdade é item do checklist pré-produção: sem ele o usuário só vê o
/// alerta quando abre o painel, o que reduz — mas não elimina — o valor da rede de segurança.
/// </para>
/// </remarks>
internal sealed class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger)
    : INotificationSender
{
    public Task SendAsync(
        TenantId tenantId,
        NotificationKind kind,
        NotificationPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Aviso {Kind} para o tenant {TenantId}: {Title} ({ResourcePath}).",
                kind,
                tenantId.Value,
                payload.Title,
                payload.ResourcePath ?? "-");
        }

        return Task.CompletedTask;
    }
}
