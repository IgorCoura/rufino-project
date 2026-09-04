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
/// <summary>
/// Onde a mensagem está agora, depois de reencontrada pelo identificador permanente.
/// </summary>
/// <param name="ExternalMessageId">O id de armazenamento válido hoje.</param>
/// <param name="ArtifactKey">O id do anexo derivado dele — muda junto, sempre.</param>
public sealed record RelocatedArtifact(string ExternalMessageId, string ArtifactKey);

public interface IMailboxReader
{
    /// <summary>
    /// A chave de artefato que designa o <strong>corpo</strong> da mensagem, em vez de um anexo.
    /// Vive na porta porque a chave é persistida (em <c>capture_items</c> e no pedido de
    /// download) e a Application precisa pedi-la sem conhecer o adapter.
    /// </summary>
    const string BODY_ARTIFACT_KEY = "message-body";

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
    /// <param name="capturedSince">
    /// Piso temporal: nada recebido antes desta data é trazido. <c>null</c> = sem limite.
    /// <para>
    /// <strong>Só tem efeito quando <paramref name="cursor"/> é <c>null</c></strong>, e não por
    /// economia: o provedor grava as opções de consulta dentro do cursor que devolve, então uma
    /// varredura incremental já carrega o piso com que começou. Trocar o piso exige descartar o
    /// cursor, e quem garante isso é <c>CaptureSource.ChangeCaptureSince</c>.
    /// </para>
    /// </param>
    Task<MailboxReadResult> ReadAsync(
        string mailboxAddress,
        CredentialRef credential,
        string? folderPath,
        string? cursor,
        DateOnly? capturedSince,
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

    /// <summary>
    /// Reencontra a mensagem pelo <c>Message-ID</c> do cabeçalho e devolve os ids atuais.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Existe porque id de armazenamento morre e o do cabeçalho não.</strong> Com o
    /// <c>ImmutableId</c> ligado a causa comum desaparece, mas item movido para caixa de arquivo
    /// morto ou exportado e reimportado ainda troca de id — e aí o download repete 404 por mais
    /// que alguém reprocesse, porque o ponteiro gravado é o mesmo.
    /// </para>
    /// <para>
    /// <c>null</c> quando a mensagem não existe mais ou quando o anexo não pôde ser identificado
    /// com segurança entre os irmãos. Devolver o anexo errado trocaria o documento em silêncio,
    /// que é pior que não achar.
    /// </para>
    /// </remarks>
    /// <summary>
    /// Lê <strong>uma</strong> mensagem pelo id, com os artefatos que ela tem agora.
    /// </summary>
    /// <remarks>
    /// Serve à recaptura, que reingere um e-mail sem tocar no cursor da pasta. Reler pelo cursor
    /// traria a caixa inteira para trazer uma mensagem — e o cursor existe justamente para isso
    /// não acontecer. <c>null</c> quando a mensagem não existe mais.
    /// </remarks>
    Task<MailboxMessage?> ReadSingleMessageAsync(
        string mailboxAddress,
        CredentialRef credential,
        string externalMessageId,
        CancellationToken cancellationToken);

    Task<RelocatedArtifact?> RelocateArtifactAsync(
        string mailboxAddress,
        CredentialRef credential,
        string internetMessageId,
        string? fileName,
        CancellationToken cancellationToken);
}
