namespace BillPayment.Domain.TrustedOrigins;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Decisão explícita de um tenant sobre uma origem de captura (remetente, domínio de
/// e-mail ou domínio web). A ausência de registro é origem desconhecida — estado válido
/// e comum, deliberadamente não modelado aqui.
/// </summary>
public sealed class TrustedOrigin : AggregateRoot<TrustedOriginId>
{
    public const int VALUE_MAX_LENGTH = 320;
    public const int NOTE_MAX_LENGTH = 500;

    public TenantId TenantId { get; private set; }
    public OriginKind Kind { get; private set; } = default!;

    /// <summary>Valor normalizado (minúsculas, sem espaços). Use <see cref="Normalize"/> para produzir a chave de busca.</summary>
    public string Value { get; private set; } = string.Empty;

    public TrustDecision Decision { get; private set; } = default!;
    public UserId DecidedBy { get; private set; }
    public DateTime DecidedAt { get; private set; }
    public string? Note { get; private set; }

    private TrustedOrigin() { }

    private TrustedOrigin(TrustedOriginId id) : base(id) { }

    public static TrustedOrigin Register(
        TenantId tenantId,
        OriginKind kind,
        string value,
        TrustDecision decision,
        UserId decidedBy,
        string? note,
        DateTime occurredAt)
    {
        var origin = new TrustedOrigin(TrustedOriginId.New()) { TenantId = tenantId };

        origin.SetKind(kind);
        origin.SetValue(value);
        origin.SetDecision(decision, decidedBy, occurredAt);
        origin.SetNote(note);

        origin.CreatedAt = occurredAt;
        origin.UpdatedAt = occurredAt;
        return origin;
    }

    /// <summary>Muda a decisão sobre a origem — por exemplo, promover uma origem observada a confiável.</summary>
    public void ChangeDecision(TrustDecision decision, UserId decidedBy, string? note, DateTime occurredAt)
    {
        SetDecision(decision, decidedBy, occurredAt);
        SetNote(note);
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Produz a chave canônica de comparação. A Application e o repositório devem normalizar
    /// o candidato por aqui — normalizar em dois lugares diferentes é como a resolução passa
    /// a divergir do que foi cadastrado.
    /// </summary>
    public static string Normalize(string value) => EmailSyntax.Normalize(value);

    /// <summary>Extrai o domínio de um endereço de e-mail, já normalizado. Vazio se não houver '@'.</summary>
    public static string ExtractDomain(string emailAddress) => EmailSyntax.ExtractDomain(emailAddress);

    /// <summary>Verifica se esta origem cobre o remetente informado, respeitando a natureza do cadastro.</summary>
    public bool Matches(string senderAddress)
    {
        var normalized = Normalize(senderAddress);
        if (normalized.Length == 0)
            return false;

        if (Kind == OriginKind.EmailAddress)
            return string.Equals(Value, normalized, StringComparison.Ordinal);

        var domain = ExtractDomain(normalized);
        var candidate = domain.Length == 0 ? normalized : domain;
        return string.Equals(Value, candidate, StringComparison.Ordinal);
    }

    private void SetKind(OriginKind kind)
        => Kind = kind ?? throw TrustedOriginErrors.KindRequired();

    private void SetDecision(TrustDecision decision, UserId decidedBy, DateTime occurredAt)
    {
        if (decision is null)
            throw TrustedOriginErrors.DecisionRequired();
        if (decidedBy.Equals(UserId.Empty))
            throw TrustedOriginErrors.DecidedByRequired();

        Decision = decision;
        DecidedBy = decidedBy;
        DecidedAt = occurredAt;
    }

    private void SetValue(string value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
            throw TrustedOriginErrors.ValueRequired();
        if (normalized.Length > VALUE_MAX_LENGTH)
            throw TrustedOriginErrors.ValueTooLong(VALUE_MAX_LENGTH);

        if (Kind.RequiresAtSign)
        {
            if (!EmailSyntax.IsValidAddress(normalized))
                throw TrustedOriginErrors.InvalidEmailAddress(value);
        }
        else if (!EmailSyntax.IsValidDomain(normalized))
        {
            throw TrustedOriginErrors.InvalidDomain(value);
        }

        Value = normalized;
    }

    private void SetNote(string? note)
    {
        var trimmed = note?.Trim();
        if (trimmed is { Length: > NOTE_MAX_LENGTH })
            throw TrustedOriginErrors.NoteTooLong(NOTE_MAX_LENGTH);

        Note = string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
