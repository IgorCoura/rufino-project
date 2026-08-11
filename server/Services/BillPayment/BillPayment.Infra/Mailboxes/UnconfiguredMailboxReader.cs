namespace BillPayment.Infra.Mailboxes;

using BillPayment.Domain.Mailboxes;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;

/// <summary>
/// Substituto usado enquanto não existe adapter de caixa configurado — hoje, sempre: o
/// Microsoft Graph entra na sprint 2.2.
/// </summary>
/// <remarks>
/// <para>
/// Devolve <c>Unavailable</c>, e isso é a parte que importa. A consequência prática é que
/// <strong>conectar uma fonte falha</strong> com <c>BLP.CPS14</c> em vez de criar uma fonte que
/// nunca sincronizaria: a prova de acesso do ADR-006 não tem como passar sem alguém para
/// responder. Um substituto que dissesse "acesso concedido" deixaria o usuário com uma caixa
/// cadastrada, silenciosa e aparentemente saudável — exatamente a falha silenciosa que o
/// ADR-014 existe para evitar.
/// </para>
/// <para>
/// Também é o que faz a suíte de integração rodar sem credencial de e-mail nenhuma.
/// </para>
/// </remarks>
internal sealed class UnconfiguredMailboxReader(TimeProvider clock) : IMailboxReader
{
    public const string REASON_CODE = "mailbox_provider_not_configured";

    private const string MESSAGE =
        "Leitura de caixa indisponível: nenhum adapter de provedor de e-mail configurado.";

    public Task<MailboxAccessProbe> ProbeAccessAsync(
        string mailboxAddress,
        CredentialRef credential,
        string? folderPath,
        CancellationToken cancellationToken)
        => Task.FromResult(MailboxAccessProbe.Unavailable(REASON_CODE, MESSAGE, clock.GetUtcNow()));

    public Task<MailboxReadResult> ReadAsync(
        string mailboxAddress,
        CredentialRef credential,
        string? folderPath,
        string? cursor,
        CancellationToken cancellationToken)
        => Task.FromResult(MailboxReadResult.Unavailable(REASON_CODE, MESSAGE, clock.GetUtcNow()));

    /// <summary>Sem adapter não há de onde baixar — e nulo é o desfecho previsto, não erro.</summary>
    public Task<ReadOnlyMemory<byte>?> DownloadArtifactAsync(
        string mailboxAddress,
        CredentialRef credential,
        string externalMessageId,
        string artifactKey,
        CancellationToken cancellationToken)
        => Task.FromResult<ReadOnlyMemory<byte>?>(null);
}
