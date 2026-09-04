namespace BillPayment.Domain.Lookups;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que voltou de uma tentativa de consulta: o retrato, ou o motivo de não haver retrato.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Por que não é exceção.</strong> "O provedor não conhece este título" é resposta
/// normal, não falha: nenhuma das doze linhas de cobrança do corpus resolveu em sandbox. Fluxo
/// que acontece na maioria das vezes não se modela com exceção — e a exceção ainda colapsaria
/// "não conheço" com "não respondi", que exigem tratamentos opostos (ver <see cref="LookupStatus"/>).
/// </para>
/// <para>
/// <strong>Um tipo concreto por trilho, em vez de um genérico.</strong> São exatamente dois e
/// nunca serão mais; o genérico obrigaria as factories estáticas a viverem num tipo genérico
/// (CA1000) e deixaria as assinaturas das portas mais difíceis de ler do que o que economiza.
/// </para>
/// <para>
/// <strong>Este VO não é persistido.</strong> O que fica gravado no <c>Bill</c> é o retrato; o
/// resultado é o veículo entre o adapter e o serviço de verificação.
/// </para>
/// </remarks>
public abstract class LookupResult : ValueObject
{
    public const int REASON_CODE_MAX_LENGTH = 100;
    public const int PROVIDER_MESSAGE_MAX_LENGTH = 500;

    public LookupStatus Status { get; }

    /// <summary>
    /// Código estável do motivo, na forma que o provedor usa (<c>unregistered_bank_slip</c>,
    /// <c>timeout</c>). É por ele que se agrupa falha de consulta no relatório — a mensagem
    /// muda de redação, o código não.
    /// </summary>
    public string? ReasonCode { get; }

    /// <summary>Texto do provedor, para a evidência. Nunca é o que decide nada.</summary>
    public string? ProviderMessage { get; }

    public DateTimeOffset AttemptedAt { get; }

    protected LookupResult(LookupStatus status, string? reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
    {
        if (!status.HasSnapshot && string.IsNullOrWhiteSpace(reasonCode))
            throw LookupErrors.ReasonCodeRequired();

        Status = status;
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? null : Clamp(reasonCode, REASON_CODE_MAX_LENGTH);
        ProviderMessage = string.IsNullOrWhiteSpace(providerMessage) ? null : Clamp(providerMessage, PROVIDER_MESSAGE_MAX_LENGTH);
        AttemptedAt = attemptedAt;
    }

    public bool IsResolved => Status.HasSnapshot;

    /// <summary>Vale a pena consultar de novo? Só indisponibilidade é retentável.</summary>
    public bool IsRetryable => Status.IsRetryable;

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

/// <summary>Resultado da consulta oficial no trilho de código de barras.</summary>
public sealed class BillLookupResult : LookupResult
{
    /// <summary>Preenchido apenas quando <see cref="LookupStatus.Resolved"/>.</summary>
    public LookupSnapshot? Snapshot { get; }

    private BillLookupResult(
        LookupStatus status,
        LookupSnapshot? snapshot,
        string? reasonCode,
        string? providerMessage,
        DateTimeOffset attemptedAt)
        : base(status, reasonCode, providerMessage, attemptedAt)
        => Snapshot = snapshot;

    public static BillLookupResult Resolved(LookupSnapshot snapshot, DateTimeOffset attemptedAt)
        => snapshot is null
            ? throw LookupErrors.SnapshotRequired()
            : new BillLookupResult(LookupStatus.Resolved, snapshot, reasonCode: null, providerMessage: null, attemptedAt);

    /// <summary>O provedor respondeu e não tem o documento. Retentar não muda a resposta.</summary>
    public static BillLookupResult Unresolved(string reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
        => new(LookupStatus.Unresolved, snapshot: null, reasonCode, providerMessage, attemptedAt);

    /// <summary>Não houve resposta útil. Nada foi aprendido sobre o documento.</summary>
    public static BillLookupResult Unavailable(string reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
        => new(LookupStatus.Unavailable, snapshot: null, reasonCode, providerMessage, attemptedAt);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var component in base.GetEqualityComponents())
            yield return component;

        yield return Snapshot;
    }
}

/// <summary>Resultado do decode no trilho Pix.</summary>
public sealed class PixLookupResult : LookupResult
{
    /// <summary>Preenchido apenas quando <see cref="LookupStatus.Resolved"/>.</summary>
    public PixLookupSnapshot? Snapshot { get; }

    private PixLookupResult(
        LookupStatus status,
        PixLookupSnapshot? snapshot,
        string? reasonCode,
        string? providerMessage,
        DateTimeOffset attemptedAt)
        : base(status, reasonCode, providerMessage, attemptedAt)
        => Snapshot = snapshot;

    public static PixLookupResult Resolved(PixLookupSnapshot snapshot, DateTimeOffset attemptedAt)
        => snapshot is null
            ? throw LookupErrors.SnapshotRequired()
            : new PixLookupResult(LookupStatus.Resolved, snapshot, reasonCode: null, providerMessage: null, attemptedAt);

    public static PixLookupResult Unresolved(string reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
        => new(LookupStatus.Unresolved, snapshot: null, reasonCode, providerMessage, attemptedAt);

    public static PixLookupResult Unavailable(string reasonCode, string? providerMessage, DateTimeOffset attemptedAt)
        => new(LookupStatus.Unavailable, snapshot: null, reasonCode, providerMessage, attemptedAt);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var component in base.GetEqualityComponents())
            yield return component;

        yield return Snapshot;
    }
}
