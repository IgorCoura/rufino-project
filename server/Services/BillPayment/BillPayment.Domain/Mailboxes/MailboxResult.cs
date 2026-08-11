namespace BillPayment.Domain.Mailboxes;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que voltou de uma conversa com a caixa: o resultado, ou o motivo de não haver resultado.
/// </summary>
/// <remarks>
/// Mesma doutrina do <c>LookupResult</c>, e pelo mesmo motivo: credencial revogada, throttling e
/// cursor expirado são desfechos <strong>normais</strong> de um job que roda sozinho a cada
/// poucos minutos. Exceção aqui derrubaria a varredura das demais fontes por causa de uma.
/// </remarks>
public abstract class MailboxResult : ValueObject
{
    public const int REASON_CODE_MAX_LENGTH = 100;
    public const int PROVIDER_MESSAGE_MAX_LENGTH = 500;

    public MailboxStatus Status { get; }

    /// <summary>Código estável do motivo (<c>invalid_client</c>, <c>throttled</c>, <c>delta_token_expired</c>).</summary>
    public string? ReasonCode { get; }

    /// <summary>Texto do provedor, para diagnóstico. Nunca é o que decide nada.</summary>
    public string? ProviderMessage { get; }

    public DateTimeOffset AttemptedAt { get; }

    protected MailboxResult(
        MailboxStatus status,
        string? reasonCode,
        string? providerMessage,
        DateTimeOffset attemptedAt)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status != MailboxStatus.Ok && string.IsNullOrWhiteSpace(reasonCode))
            throw MailboxErrors.ReasonCodeRequired();

        Status = status;
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? null : Clamp(reasonCode, REASON_CODE_MAX_LENGTH);
        ProviderMessage = string.IsNullOrWhiteSpace(providerMessage)
            ? null
            : Clamp(providerMessage, PROVIDER_MESSAGE_MAX_LENGTH);
        AttemptedAt = attemptedAt;
    }

    public bool IsOk => Status == MailboxStatus.Ok;

    /// <summary>A próxima tentativa precisa descartar o cursor e varrer a caixa inteira?</summary>
    public bool RequiresCursorReset => Status.RequiresCursorReset;

    protected static string Clamp(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Status;
        yield return ReasonCode;
        yield return ProviderMessage;
        yield return AttemptedAt;
    }
}

/// <summary>
/// A prova de que a credencial alcança aquela caixa.
/// </summary>
/// <remarks>
/// É o que substitui o "concluir o OAuth" no modelo de client credentials do ADR-006: não há
/// tela de consentimento por fonte, então quem prova o acesso é uma leitura de teste. Só depois
/// dela a <c>CaptureSource</c> é criada e o aviso de caixa compartilhada aparece — perguntar
/// antes transformaria o endpoint num oráculo de endereços cadastrados (ADR-008).
/// </remarks>
public sealed class MailboxAccessProbe : MailboxResult
{
    private MailboxAccessProbe(
        MailboxStatus status,
        string? reasonCode,
        string? providerMessage,
        DateTimeOffset attemptedAt)
        : base(status, reasonCode, providerMessage, attemptedAt) { }

    public static MailboxAccessProbe Granted(DateTimeOffset attemptedAt)
        => new(MailboxStatus.Ok, reasonCode: null, providerMessage: null, attemptedAt);

    public static MailboxAccessProbe Denied(string reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
        => new(MailboxStatus.Denied, reasonCode, providerMessage, attemptedAt);

    public static MailboxAccessProbe Unavailable(string reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
        => new(MailboxStatus.Unavailable, reasonCode, providerMessage, attemptedAt);
}

/// <summary>O que uma varredura incremental trouxe, e de onde continuar na próxima.</summary>
public sealed class MailboxReadResult : MailboxResult
{
    private readonly List<MailboxMessage> _messages;

    /// <summary>Vazio em qualquer desfecho que não seja <c>Ok</c>.</summary>
    public IReadOnlyList<MailboxMessage> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Onde continuar. <c>null</c> significa que a próxima varredura é completa — e só o
    /// agregado decide se guarda isso, via <c>RecordSyncSuccess</c>.
    /// </summary>
    public string? NextCursor { get; }

    private MailboxReadResult(
        MailboxStatus status,
        List<MailboxMessage> messages,
        string? nextCursor,
        string? reasonCode,
        string? providerMessage,
        DateTimeOffset attemptedAt)
        : base(status, reasonCode, providerMessage, attemptedAt)
    {
        _messages = messages;
        NextCursor = nextCursor;
    }

    public static MailboxReadResult Ok(
        IEnumerable<MailboxMessage> messages,
        string? nextCursor,
        DateTimeOffset attemptedAt)
    {
        ArgumentNullException.ThrowIfNull(messages);

        return new MailboxReadResult(
            MailboxStatus.Ok, messages.ToList(), nextCursor, reasonCode: null, providerMessage: null, attemptedAt);
    }

    public static MailboxReadResult Denied(string reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
        => new(MailboxStatus.Denied, [], nextCursor: null, reasonCode, providerMessage, attemptedAt);

    /// <summary>O provedor invalidou o cursor. A resposta é descartá-lo, não retentar igual.</summary>
    public static MailboxReadResult CursorExpired(string reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
        => new(MailboxStatus.CursorExpired, [], nextCursor: null, reasonCode, providerMessage, attemptedAt);

    public static MailboxReadResult Unavailable(string reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
        => new(MailboxStatus.Unavailable, [], nextCursor: null, reasonCode, providerMessage, attemptedAt);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var component in base.GetEqualityComponents())
            yield return component;

        yield return NextCursor;

        foreach (var message in _messages)
            yield return message;
    }
}
