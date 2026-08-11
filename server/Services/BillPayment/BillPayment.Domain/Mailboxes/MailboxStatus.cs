namespace BillPayment.Domain.Mailboxes;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Como terminou a conversa com o provedor da caixa.
/// </summary>
/// <remarks>
/// <para>
/// Mesma doutrina do <c>LookupStatus</c>: falha de integração é <strong>modelada</strong>, não
/// lançada. Mas os modos de falha são outros, e é por isso que este tipo existe separado.
/// </para>
/// <para>
/// <see cref="CursorExpired"/> merece valor próprio porque a resposta a ele é <em>diferente</em>
/// de retentar: é descartar o cursor e varrer a caixa inteira. Colapsá-lo em
/// <see cref="Unavailable"/> faria a fonte parar de sincronizar em silêncio — a pior falha
/// possível num sistema em que ninguém garante que a conta chegou (ADR-014).
/// </para>
/// </remarks>
public sealed class MailboxStatus : Enumeration
{
    /// <summary>A caixa respondeu.</summary>
    public static readonly MailboxStatus Ok = new(1, nameof(Ok), isRetryable: false, requiresCursorReset: false);

    /// <summary>
    /// A credencial foi rejeitada, ou não alcança esta caixa. É fato sobre a
    /// <strong>autorização</strong>, não sobre a rede: retentar dá a mesma resposta, e quem
    /// resolve é uma pessoa mexendo no registro de aplicativo ou na Application Access Policy.
    /// </summary>
    public static readonly MailboxStatus Denied = new(2, nameof(Denied), isRetryable: false, requiresCursorReset: false);

    /// <summary>
    /// O cursor ficou velho demais e o provedor o invalidou (<c>410 Gone</c> no Graph). Não é
    /// erro: é a deixa para varrer a caixa inteira uma vez e recomeçar o incremental.
    /// </summary>
    public static readonly MailboxStatus CursorExpired =
        new(3, nameof(CursorExpired), isRetryable: true, requiresCursorReset: true);

    /// <summary>Timeout, 5xx, throttling, circuito aberto, credencial ausente. Nada foi lido.</summary>
    public static readonly MailboxStatus Unavailable =
        new(4, nameof(Unavailable), isRetryable: true, requiresCursorReset: false);

    /// <summary>Vale a pena tentar de novo mais tarde?</summary>
    public bool IsRetryable { get; }

    /// <summary>A próxima tentativa precisa começar do zero, sem cursor?</summary>
    public bool RequiresCursorReset { get; }

    private MailboxStatus(int id, string name, bool isRetryable, bool requiresCursorReset) : base(id, name)
    {
        IsRetryable = isRetryable;
        RequiresCursorReset = requiresCursorReset;
    }
}
