namespace BillPayment.Domain.Ports;

using BillPayment.Domain.SharedKernel;

/// <summary>O que o aviso comunica. Decide o texto e a ação oferecida na outra ponta.</summary>
public enum NotificationKind
{
    /// <summary>Passei a monitorar esta conta por conta própria.</summary>
    ExpectationLearned = 1,

    /// <summary>A conta não chegou — vá buscar.</summary>
    ExpectationMissing = 2,

    /// <summary>A conta chegou e não consegui ler — resolva o item.</summary>
    ExpectationCaptureFailed = 3,
}

/// <param name="Title">Linha única, já legível — quem monta é a Application.</param>
/// <param name="Body">O corpo do aviso, sem nenhum dado pagável.</param>
/// <param name="ResourcePath">
/// Caminho relativo do que resolve o aviso, quando existe: o item na quarentena, a expectativa.
/// É o que transforma alerta em ação de um clique.
/// </param>
public sealed record NotificationPayload(string Title, string Body, string? ResourcePath = null);

/// <summary>
/// Leva um aviso ao tenant.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nenhum instrumento de pagamento atravessa esta porta.</strong> Linha digitável e BR
/// Code são meios de pagamento — quem tem, paga —, e um aviso trafega por canal que o BC não
/// controla. O aviso diz o que aconteceu e para onde ir; o dado fica atrás da autenticação.
/// </para>
/// <para>
/// <strong>Falha de envio não pode desfazer o registro do alerta.</strong> Quem grava que o nível
/// já saiu é o agregado, na mesma transação do efeito; o envio acontece depois, pelo outbox. Se
/// o canal estiver fora do ar, o painel de pendências continua contando a verdade.
/// </para>
/// </remarks>
public interface INotificationSender
{
    Task SendAsync(
        TenantId tenantId,
        NotificationKind kind,
        NotificationPayload payload,
        CancellationToken cancellationToken);
}
