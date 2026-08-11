namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Mailboxes;
using BillPayment.Domain.Secrets;

/// <summary>
/// Leitura de uma caixa de e-mail monitorada. Só existe um adapter — Microsoft Graph; conta
/// pessoal entra por encaminhamento, sem integração própria (ADR-006).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nunca lança por falha de integração.</strong> Credencial revogada, throttling e
/// cursor expirado são desfechos normais de um job que roda sozinho — ver
/// <see cref="MailboxStatus"/>. Exceção aqui derrubaria a varredura das outras fontes por causa
/// de uma. Exceção significa defeito de programação.
/// </para>
/// <para>
/// <strong>Só leitura, e só metadado.</strong> A porta não apaga, não marca como lida e não
/// move mensagem: o sistema não altera a caixa de ninguém. E não traz os bytes dos anexos —
/// baixar é chamada à parte, na sprint 2.3, para uma varredura não arrastar megabytes que talvez
/// nem sejam boleto.
/// </para>
/// <para>
/// A credencial entra como <see cref="CredentialRef"/>, nunca como segredo: quem resolve o
/// ponteiro é o adapter, contra o <c>ISecretVault</c>, já do lado de fora do Domain.
/// </para>
/// </remarks>
public interface IMailboxReader
{
    /// <summary>
    /// Prova que a credencial alcança a caixa, antes de a fonte passar a existir.
    /// </summary>
    /// <remarks>
    /// No modelo de client credentials não há tela de consentimento por fonte, então esta
    /// leitura de teste é o que faz o papel do "OAuth concluiu" exigido pelo ADR-008 — e é o
    /// que garante que o aviso de caixa compartilhada só apareça para quem provou controlar
    /// aquele endereço.
    /// </remarks>
    /// <param name="folderPath">
    /// Pasta a monitorar; <c>null</c> é a caixa de entrada. A prova cobre a pasta, e não só a
    /// caixa: uma fonte apontada para pasta inexistente falharia em silêncio na primeira
    /// varredura, devolvendo zero mensagens sem nenhum erro.
    /// </param>
    Task<MailboxAccessProbe> ProbeAccessAsync(
        string mailboxAddress,
        CredentialRef credential,
        string? folderPath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Traz o que mudou desde <paramref name="cursor"/>. <c>null</c> pede varredura completa.
    /// </summary>
    /// <remarks>
    /// O cursor é <strong>por pasta</strong>: trocar de pasta invalida o cursor anterior, e quem
    /// garante isso é <c>CaptureSource.ChangeFolder</c>.
    /// </remarks>
    Task<MailboxReadResult> ReadAsync(
        string mailboxAddress,
        CredentialRef credential,
        string? folderPath,
        string? cursor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Traz os bytes de <strong>um</strong> artefato, sob demanda.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Separado da varredura de propósito: <see cref="ReadAsync"/> lista metadado de dezenas de
    /// mensagens e arrastar o conteúdo junto faria cada ciclo do agendador puxar megabytes que
    /// em boa parte nem são boleto. Aqui o custo é pago por artefato, quando ele já foi
    /// escolhido para processamento.
    /// </para>
    /// <para>
    /// <c>null</c> significa que o artefato não veio — apagado, grande demais para o provedor,
    /// ou indisponível no momento. Não é exceção porque a varredura não pode parar por causa de
    /// um anexo.
    /// </para>
    /// </remarks>
    Task<ReadOnlyMemory<byte>?> DownloadArtifactAsync(
        string mailboxAddress,
        CredentialRef credential,
        string externalMessageId,
        string artifactKey,
        CancellationToken cancellationToken);
}
