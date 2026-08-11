namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Domain.Mailboxes;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;

/// <summary>
/// Caixa de e-mail falsa e programável.
/// </summary>
/// <remarks>
/// <para>
/// Existe para exercitar caminhos que sem ela seriam inalcançáveis enquanto o adapter do Graph
/// não existe (sprint 2.2): conectar uma fonte com acesso provado, e a varredura trazendo
/// mensagens.
/// </para>
/// <para>
/// <strong>Não é mock de comportamento.</strong> Devolve o que foi programado e registra o que
/// recebeu; nenhum teste asserta "foi chamado" — quem prova a orquestração é o efeito no banco.
/// O <see cref="LastCursor"/> é a exceção justificada: é a única forma de provar que a
/// sincronização seguinte retoma de onde a anterior parou, e isso não aparece em lugar nenhum
/// além do argumento.
/// </para>
/// </remarks>
internal sealed class FakeMailboxReader : IMailboxReader
{
    public static readonly DateTimeOffset AttemptedAt = new(2026, 8, 10, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Por padrão concede acesso — é o caminho feliz de conectar uma fonte.</summary>
    public MailboxAccessProbe ProbeResult { get; set; } = MailboxAccessProbe.Granted(AttemptedAt);

    /// <summary>Por padrão não traz nada, que é o desfecho comum de uma varredura.</summary>
    public MailboxReadResult ReadResult { get; set; } =
        MailboxReadResult.Ok([], nextCursor: "deltaLink-1", AttemptedAt);

    /// <summary>O cursor que a última varredura recebeu. <c>null</c> = varredura completa.</summary>
    public string? LastCursor { get; private set; }

    /// <summary>A pasta que a última varredura recebeu. <c>null</c> = caixa de entrada.</summary>
    public string? LastFolderPath { get; private set; }

    public int ReadCount { get; private set; }

    public Task<MailboxAccessProbe> ProbeAccessAsync(
        string mailboxAddress,
        CredentialRef credential,
        string? folderPath,
        CancellationToken cancellationToken)
        => Task.FromResult(ProbeResult);

    public Task<MailboxReadResult> ReadAsync(
        string mailboxAddress,
        CredentialRef credential,
        string? folderPath,
        string? cursor,
        CancellationToken cancellationToken)
    {
        LastFolderPath = folderPath;
        LastCursor = cursor;
        ReadCount++;
        return Task.FromResult(ReadResult);
    }

    /// <summary>Conteúdo devolvido no download, por chave de artefato.</summary>
    public Dictionary<string, byte[]> Artifacts { get; } = new(StringComparer.Ordinal);

    public Task<ReadOnlyMemory<byte>?> DownloadArtifactAsync(
        string mailboxAddress,
        CredentialRef credential,
        string externalMessageId,
        string artifactKey,
        CancellationToken cancellationToken)
    {
        // Nulo explicito: `cond ? bytes : null` converteria um byte[] nulo em ReadOnlyMemory
        // VAZIO — nao nulo —, e o teste de falha de download passaria a exercitar outro caminho.
        if (!Artifacts.TryGetValue(artifactKey, out var bytes))
            return Task.FromResult<ReadOnlyMemory<byte>?>(null);

        return Task.FromResult<ReadOnlyMemory<byte>?>(bytes);
    }

    /// <summary>Monta uma mensagem com N artefatos, para o caso do e-mail com vários boletos.</summary>
    public static MailboxMessage Message(string messageId, params string[] artifactKeys)
        => MailboxMessage.From(
            messageId,
            "faturas@enel.com.br",
            "Sua fatura de energia chegou",
            AttemptedAt.AddHours(-1),
            artifactKeys.Select(key => MailboxArtifact.From(key, key, "application/pdf", 1024)));
}
